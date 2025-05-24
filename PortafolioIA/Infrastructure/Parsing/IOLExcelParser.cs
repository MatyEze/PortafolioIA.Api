using Application.Interfaces;
using Domain.Entities;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Infrastructure.Parsing
{
    public class IOLExcelParser : IFileParsingService
    {
        private static readonly string[] SupportedBrokers = { "IOL" };
        private static readonly string[] SupportedExtensions = { ".xls", ".html", ".htm" }; // IOL usa .xls pero son HTML

        public bool CanParse(string brokerKey, string fileName)
        {
            return SupportedBrokers.Contains(brokerKey.ToUpperInvariant()) &&
                   SupportedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());
        }

        public IEnumerable<string> GetSupportedBrokers() => SupportedBrokers;

        public IEnumerable<string> GetSupportedExtensions() => SupportedExtensions;

        public async Task<ParsingResult> ParseFileAsync(Stream fileStream, string fileName, string brokerKey, Guid dataPointId)
        {
            var result = new ParsingResult();
            var statistics = new ParsingStatistics();

            try
            {
                // PASO 1: Validar el stream
                fileStream.Position = 0;
                if (fileStream.Length == 0)
                {
                    result.Errores.Add("El archivo está vacío");
                    return result;
                }

                // PASO 2: Verificar que es un archivo HTML de IOL
                fileStream.Position = 0;
                using var reader = new StreamReader(fileStream, leaveOpen: true);

                await reader.ReadLineAsync(); // Primera línea (vacía)
                var secondLine = await reader.ReadLineAsync(); // Segunda línea con signature
                fileStream.Position = 0;

                if (secondLine == null || !secondLine.Contains("/Content/IOLV6Screens"))
                {
                    result.Errores.Add("El archivo no parece ser un extracto válido de IOL. No se encontró la signature esperada.");
                    return result;
                }

                result.Advertencias.Add("✅ Detectado: Archivo HTML de IOL");

                // PASO 3: Leer todo el contenido HTML
                using var fullReader = new StreamReader(fileStream);
                var htmlContent = await fullReader.ReadToEndAsync();

                result.Advertencias.Add($"📄 Contenido HTML: {htmlContent.Length} caracteres");

                // PASO 4: Encontrar y extraer la tabla
                var tableContent = ExtractTableContent(htmlContent);
                if (string.IsNullOrEmpty(tableContent))
                {
                    result.Errores.Add("No se encontró una tabla HTML válida en el archivo");
                    return result;
                }

                result.Advertencias.Add("📋 Tabla HTML encontrada");

                // PASO 5: Extraer filas de la tabla
                var rows = ExtractTableRows(tableContent);
                if (rows.Count < 2) // Al menos header + 1 fila de datos
                {
                    result.Errores.Add($"La tabla solo tiene {rows.Count} fila(s). Se necesitan al menos 2 (header + datos)");
                    return result;
                }

                result.Advertencias.Add($"📊 Filas extraídas: {rows.Count} (incluyendo header)");

                // PASO 6: Validar headers
                var headers = ExtractRowData(rows[0]);
                if (!ValidateHeaders(headers))
                {
                    result.Errores.Add("Los headers de la tabla no coinciden con el formato esperado de IOL");
                    result.Advertencias.Add($"Headers encontrados: {string.Join(" | ", headers.Take(5))}...");
                    return result;
                }

                result.Advertencias.Add("✅ Headers validados correctamente");

                // PASO 7: Procesar filas de datos (omitir header)
                var culture = new CultureInfo("es-ES");
                statistics.TotalFilasProcessadas = rows.Count - 1;

                for (int i = 1; i < rows.Count; i++)
                {
                    try
                    {
                        var rowData = ExtractRowData(rows[i]);
                        if (rowData.Count < 14) // IOL tiene 14 columnas
                        {
                            statistics.FilasIgnoradas++;
                            continue;
                        }

                        var movimiento = ProcessRowData(rowData, dataPointId, brokerKey, culture);
                        if (movimiento != null)
                        {
                            result.Movimientos.Add(movimiento);
                            statistics.FilasExitosas++;
                            UpdateStatistics(statistics, movimiento);
                        }
                        else
                        {
                            statistics.FilasIgnoradas++;
                        }
                    }
                    catch (Exception ex)
                    {
                        statistics.FilasConErrores++;
                        result.Advertencias.Add($"Error en fila {i + 1}: {ex.Message}");
                    }
                }

                result.Statistics = statistics;

                if (result.Movimientos.Count == 0)
                {
                    result.Errores.Add("No se pudo procesar ningún movimiento válido del archivo");
                }
                else
                {
                    result.Advertencias.Add($"✅ Procesamiento completado: {result.Movimientos.Count} movimientos extraídos");
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Errores.Add($"Error general al procesar archivo HTML: {ex.Message}");
                if (ex.InnerException != null)
                {
                    result.Errores.Add($"Error interno: {ex.InnerException.Message}");
                }
                return result;
            }
        }

        #region Métodos de Extracción HTML

        private static string ExtractTableContent(string htmlContent)
        {
            var tableStart = htmlContent.IndexOf("<table", StringComparison.OrdinalIgnoreCase);
            var tableEnd = htmlContent.IndexOf("</table>", StringComparison.OrdinalIgnoreCase);

            if (tableStart == -1 || tableEnd == -1)
                return string.Empty;

            return htmlContent.Substring(tableStart, tableEnd - tableStart + 8);
        }

        private static List<string> ExtractTableRows(string tableContent)
        {
            var rows = new List<string>();
            var regex = new Regex(@"<tr[^>]*>(.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var matches = regex.Matches(tableContent);

            foreach (Match match in matches)
            {
                rows.Add(match.Groups[1].Value);
            }

            return rows;
        }

        private static List<string> ExtractRowData(string rowContent)
        {
            var cells = new List<string>();

            // Extraer contenido de celdas <td> y <th>
            var regex = new Regex(@"<(?:td|th)[^>]*>(.*?)</(?:td|th)>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var matches = regex.Matches(rowContent);

            foreach (Match match in matches)
            {
                var cellContent = match.Groups[1].Value;
                // Limpiar HTML tags restantes y decodificar entidades
                cellContent = Regex.Replace(cellContent, @"<[^>]+>", "");
                cellContent = System.Net.WebUtility.HtmlDecode(cellContent);
                cellContent = cellContent.Trim();
                cells.Add(cellContent);
            }

            return cells;
        }

        #endregion

        #region Validación y Procesamiento

        private static bool ValidateHeaders(List<string> headers)
        {
            if (headers.Count < 10) return false;

            // Verificar que contiene algunos headers clave de IOL
            var expectedHeaders = new[] { "Nro. de Mov", "Tipo Mov", "Concert", "Precio", "Monto" };
            int foundHeaders = 0;

            foreach (var expected in expectedHeaders)
            {
                if (headers.Any(h => h.Contains(expected, StringComparison.OrdinalIgnoreCase)))
                {
                    foundHeaders++;
                }
            }

            return foundHeaders >= 3; // Al menos 3 de los 5 headers esperados
        }

        private static Movimiento? ProcessRowData(List<string> rowData, Guid dataPointId, string brokerKey, CultureInfo culture)
        {
            try
            {
                // Mapear datos según las columnas esperadas de IOL
                var nroMovimiento = ParseInt(rowData[0]);
                var nroBoleto = rowData.Count > 1 ? rowData[1] : "";
                var tipoMovStr = rowData.Count > 2 ? rowData[2] : "";
                var fechaConcertacion = rowData.Count > 3 ? ParseFecha(rowData[3], culture) : DateTime.MinValue;
                var fechaLiquidacion = rowData.Count > 4 ? ParseFecha(rowData[4], culture) : DateTime.MinValue;
                var cantidad = rowData.Count > 6 ? ParseInt(rowData[6]) : 0;
                var precio = rowData.Count > 7 ? ParseDecimal(rowData[7], culture) : 0;
                var comision = rowData.Count > 8 ? ParseDecimal(rowData[8], culture) : 0;
                var ivaComision = rowData.Count > 9 ? ParseDecimal(rowData[9], culture) : 0;
                var otrosImpuestos = rowData.Count > 10 ? ParseDecimal(rowData[10], culture) : 0;
                var montoTotal = rowData.Count > 11 ? ParseDecimal(rowData[11], culture) : 0;
                var observaciones = rowData.Count > 12 ? rowData[12] : "";
                var tipoCuenta = rowData.Count > 13 ? rowData[13] : "";

                // Validaciones básicas
                if (nroMovimiento <= 0 || string.IsNullOrEmpty(tipoMovStr))
                    return null;

                // Procesar tipo de movimiento y ticker
                var (tipoMovimiento, ticker) = ParseTipoMovimientoYTicker(tipoMovStr);
                var tipoMoneda = ParseTipoMoneda(tipoCuenta);

                // Crear movimiento
                return Movimiento.Create(
                    dataPointId: dataPointId,
                    numeroMovimiento: nroMovimiento,
                    broker: brokerKey,
                    tipo: tipoMovimiento,
                    fechaConcertacion: fechaConcertacion,
                    fechaLiquidacion: fechaLiquidacion,
                    cantidad: Math.Abs(cantidad),
                    precio: precio,
                    comision: comision,
                    montoTotal: montoTotal,
                    moneda: tipoMoneda,
                    ticker: ticker,
                    ivaComision: ivaComision,
                    otrosImpuestos: otrosImpuestos,
                    observaciones: observaciones);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error procesando datos de fila: {ex.Message}", ex);
            }
        }

        #endregion

        #region Métodos de Parsing

        private static (TipoMovimiento tipo, string? ticker) ParseTipoMovimientoYTicker(string tipoMovStr)
        {
            if (string.IsNullOrEmpty(tipoMovStr))
                return (TipoMovimiento.Otro, null);

            // Extraer ticker de entre paréntesis
            string? ticker = null;
            var match = Regex.Match(tipoMovStr, @"\(([^)]+)\)");
            if (match.Success)
            {
                ticker = match.Groups[1].Value.Trim();
            }

            // Determinar tipo de movimiento
            var tipoLimpio = tipoMovStr.Split('(')[0].Trim().ToLowerInvariant();

            return tipoLimpio switch
            {
                var s when s.Contains("compra") => (TipoMovimiento.Compra, ticker),
                var s when s.Contains("venta") => (TipoMovimiento.Venta, ticker),
                var s when s.Contains("depósito") || s.Contains("deposito") => (TipoMovimiento.Deposito, null),
                var s when s.Contains("extracción") || s.Contains("extraccion") => (TipoMovimiento.Extraccion, null),
                var s when s.Contains("dividendo") => (TipoMovimiento.Dividendos, ticker),
                var s when s.Contains("caución") || s.Contains("caucion") => (TipoMovimiento.Caucion, null),
                var s when s.Contains("liquidación") || s.Contains("liquidacion") => (TipoMovimiento.LiquidacionCaucion, null),
                var s when s.Contains("suscripción") || s.Contains("suscripcion") => (TipoMovimiento.SuscripcionFondo, ticker),
                var s when s.Contains("rescate") => (TipoMovimiento.RescateFondo, ticker),
                var s when s.Contains("crédito") || s.Contains("credito") => (TipoMovimiento.Credito, null),
                _ => (TipoMovimiento.Otro, ticker)
            };
        }

        private static TipoMoneda ParseTipoMoneda(string tipoCuenta)
        {
            if (string.IsNullOrEmpty(tipoCuenta))
                return TipoMoneda.PesoArgentino;

            return tipoCuenta.ToLowerInvariant() switch
            {
                var s when s.Contains("pesos") => TipoMoneda.PesoArgentino,
                var s when s.Contains("dolares") || s.Contains("dólares") => TipoMoneda.DolarEstadounidense,
                var s when s.Contains("euros") => TipoMoneda.Euro,
                _ => TipoMoneda.PesoArgentino
            };
        }

        private static DateTime ParseFecha(string? fechaStr, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(fechaStr))
                return DateTime.MinValue;

            if (DateTime.TryParse(fechaStr, culture, DateTimeStyles.None, out DateTime fecha))
                return fecha;

            return DateTime.MinValue;
        }

        private static int ParseInt(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            return int.TryParse(value.Replace(".", "").Replace(",", ""), out int result) ? result : 0;
        }

        private static decimal ParseDecimal(string? value, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            return decimal.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign, culture, out decimal result) ? result : 0;
        }

        private static void UpdateStatistics(ParsingStatistics statistics, Movimiento movimiento)
        {
            var tipoKey = movimiento.Tipo.ToString();
            if (statistics.MovimientosPorTipo.ContainsKey(tipoKey))
                statistics.MovimientosPorTipo[tipoKey]++;
            else
                statistics.MovimientosPorTipo[tipoKey] = 1;

            if (statistics.FechaMasAntigua == null || movimiento.FechaConcertacion < statistics.FechaMasAntigua)
                statistics.FechaMasAntigua = movimiento.FechaConcertacion;

            if (statistics.FechaMasReciente == null || movimiento.FechaConcertacion > statistics.FechaMasReciente)
                statistics.FechaMasReciente = movimiento.FechaConcertacion;
        }

        #endregion
    }
}