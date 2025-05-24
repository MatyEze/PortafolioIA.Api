using Application.Interfaces;
using Domain.Entities;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace Infrastructure.Parsing
{
    public class IOLExcelParser : IFileParsingService
    {
        private static readonly string[] SupportedBrokers = { "IOL" };
        private static readonly string[] SupportedExtensions = { ".xlsx", ".xls" };

        // Headers esperados para IOL
        private static readonly Dictionary<string, int> ExpectedHeaders = new()
        {
            { "Nro. de Mov.", 0 },      // A
            { "Nro. de Boleto", 1 },    // B
            { "Tipo Mov.", 2 },         // C
            { "Concert.", 3 },          // D
            { "Liquid.", 4 },           // E
            { "Est", 5 },               // F
            { "Cant. titulos", 6 },     // G
            { "Precio", 7 },            // H
            { "Comis.", 8 },            // I
            { "Iva Com.", 9 },          // J
            { "Otros Imp.", 10 },       // K
            { "Monto", 11 },            // L
            { "Observaciones", 12 },    // M
            { "Tipo Cuenta", 13 }       // N
        };

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
                // 🔧 Resetear posición del stream
                fileStream.Position = 0;

                // 🎯 Replicar lógica de método JavaScript
                var dataArrayString = await ConvertExcelToStringArray(fileStream, fileName);

                if (dataArrayString.Count == 0)
                {
                    result.Errores.Add("El archivo no contiene datos o no se pudo procesar");
                    return result;
                }

                // Validar headers (primera fila)
                if (!ValidateHeaders(dataArrayString[0], result))
                {
                    return result;
                }

                var culture = new CultureInfo("es-ES");
                statistics.TotalFilasProcessadas = dataArrayString.Count - 1; // Menos el header

                // Procesar filas de datos (empezar desde índice 1)
                for (int i = 1; i < dataArrayString.Count; i++)
                {
                    try
                    {
                        var movimiento = await ProcessRowFromString(dataArrayString[i], dataPointId, brokerKey, culture);

                        if (movimiento != null)
                        {
                            result.Movimientos.Add(movimiento);
                            statistics.FilasExitosas++;

                            // Actualizar estadísticas
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
                    result.Errores.Add("No se pudo procesar ningún movimiento del archivo");
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Errores.Add($"Error general al procesar el archivo: {ex.Message}");
                return result;
            }
        }

        // 🔧 Método que replica tu lógica JavaScript
        private static async Task<List<string>> ConvertExcelToStringArray(Stream fileStream, string fileName)
        {
            var dataArrayString = new List<string>();

            try
            {
                IWorkbook workbook;
                var extension = Path.GetExtension(fileName).ToLowerInvariant();

                // Crear workbook según extensión (como SheetJS detecta automáticamente)
                if (extension == ".xlsx")
                {
                    workbook = new XSSFWorkbook(fileStream);
                }
                else if (extension == ".xls")
                {
                    workbook = new HSSFWorkbook(fileStream);
                }
                else
                {
                    throw new ArgumentException($"Extensión no soportada: {extension}");
                }

                // Obtener primera hoja (equivalente a workbook.SheetNames[0])
                var worksheet = workbook.GetSheetAt(0);

                if (worksheet == null)
                {
                    return dataArrayString;
                }

                // Leer todas las filas (equivalente a sheet_to_json con header: 1)
                var firstRowNum = worksheet.FirstRowNum;
                var lastRowNum = worksheet.LastRowNum;

                for (int rowIndex = firstRowNum; rowIndex <= lastRowNum; rowIndex++)
                {
                    var row = worksheet.GetRow(rowIndex);
                    if (row == null) continue;

                    var rowData = new List<string>();
                    var lastCellNum = Math.Max((int)row.LastCellNum, 14); // Al menos 14 columnas para IOL

                    // Leer todas las celdas de la fila
                    for (int cellIndex = 0; cellIndex < lastCellNum; cellIndex++)
                    {
                        var cell = row.GetCell(cellIndex);
                        var cellValue = GetCellValueAsString(cell);
                        rowData.Add(cellValue);
                    }

                    // Convertir fila a string con separador " | " (igual que tu JS)
                    var rowString = string.Join(" | ", rowData);
                    dataArrayString.Add(rowString);
                }

                return dataArrayString;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error leyendo Excel: {ex.Message}", ex);
            }
        }

        // Helper para obtener valor de celda como string (equivalente a raw: true en JS)
        private static string GetCellValueAsString(ICell? cell)
        {
            if (cell == null) return "";

            return cell.CellType switch
            {
                CellType.String => cell.StringCellValue ?? "",
                CellType.Numeric => DateUtil.IsCellDateFormatted(cell)
                    ? cell.DateCellValue!.Value.ToString("dd/MM/yyyy")
                    : cell.NumericCellValue.ToString(),
                CellType.Boolean => cell.BooleanCellValue.ToString(),
                CellType.Formula => GetFormulaValueAsString(cell),
                CellType.Blank => "",
                _ => ""
            };
        }

        private static string GetFormulaValueAsString(ICell cell)
        {
            try
            {
                return cell.CachedFormulaResultType switch
                {
                    CellType.String => cell.StringCellValue ?? "",
                    CellType.Numeric => cell.NumericCellValue.ToString(),
                    CellType.Boolean => cell.BooleanCellValue.ToString(),
                    _ => ""
                };
            }
            catch
            {
                return "";
            }
        }

        private static bool ValidateHeaders(string headerRow, ParsingResult result)
        {
            try
            {
                var headers = headerRow.Split(" | ");
                var expectedHeadersFound = 0;

                for (int i = 0; i < Math.Min(headers.Length, ExpectedHeaders.Count); i++)
                {
                    var headerValue = headers[i]?.Trim();
                    if (!string.IsNullOrEmpty(headerValue))
                    {
                        var expectedHeader = ExpectedHeaders.FirstOrDefault(h => h.Value == i);
                        if (expectedHeader.Key != null && headerValue.Contains(expectedHeader.Key.Split('.')[0]))
                        {
                            expectedHeadersFound++;
                        }
                    }
                }

                if (expectedHeadersFound < ExpectedHeaders.Count / 2) // Al menos la mitad de headers
                {
                    result.Errores.Add("El archivo no parece ser un extracto válido de IOL. Headers no reconocidos.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                result.Errores.Add($"Error validando headers: {ex.Message}");
                return false;
            }
        }

        private static async Task<Movimiento?> ProcessRowFromString(string rowString, Guid dataPointId, string brokerKey, CultureInfo culture)
        {
            try
            {
                // Dividir la fila por el separador " | "
                var columns = rowString.Split(" | ");

                if (columns.Length < 14) // IOL tiene 14 columnas
                {
                    return null;
                }

                // Leer valores usando los índices conocidos (igual que tu JS)
                var nroMovimiento = ParseInt(columns[0]);
                var tipoMovStr = columns[2]?.Trim() ?? "";
                var fechaConcertacion = ParseFecha(columns[3], culture);
                var fechaLiquidacion = ParseFecha(columns[4], culture);
                var cantidad = ParseInt(columns[6]);
                var precio = ParseDecimal(columns[7], culture);
                var comision = ParseDecimal(columns[8], culture);
                var ivaComision = ParseDecimal(columns[9], culture);
                var otrosImpuestos = ParseDecimal(columns[10], culture);
                var montoTotal = ParseDecimal(columns[11], culture);
                var tipoCuenta = columns[13]?.Trim() ?? "";
                var observaciones = columns[12]?.Trim();

                // Validaciones básicas
                if (nroMovimiento <= 0)
                    return null;

                // Procesar tipo de movimiento y ticker
                var (tipoMovimiento, ticker) = ParseTipoMovimientoYTicker(tipoMovStr);
                var tipoMoneda = ParseTipoMoneda(tipoCuenta);

                // Crear movimiento
                var movimiento = Movimiento.Create(
                    dataPointId: dataPointId,
                    numeroMovimiento: nroMovimiento,
                    broker: brokerKey,
                    tipo: tipoMovimiento,
                    fechaConcertacion: fechaConcertacion,
                    fechaLiquidacion: fechaLiquidacion,
                    cantidad: cantidad,
                    precio: precio,
                    comision: comision,
                    montoTotal: montoTotal,
                    moneda: tipoMoneda,
                    ticker: ticker,
                    ivaComision: ivaComision,
                    otrosImpuestos: otrosImpuestos,
                    observaciones: observaciones);

                return movimiento;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error procesando fila: {ex.Message}", ex);
            }
        }

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

            // Determinar tipo de movimiento basado en el texto antes del paréntesis
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
            return tipoCuenta.ToLowerInvariant() switch
            {
                var s when s.Contains("pesos") => TipoMoneda.PesoArgentino,
                var s when s.Contains("dolares") || s.Contains("dólares") => TipoMoneda.DolarEstadounidense,
                var s when s.Contains("euros") => TipoMoneda.Euro,
                _ => TipoMoneda.PesoArgentino // Default
            };
        }

        private static DateTime ParseFecha(string? fechaStr, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(fechaStr))
                return DateTime.MinValue;

            // Intentar parsear como fecha normal
            if (DateTime.TryParse(fechaStr, culture, DateTimeStyles.None, out DateTime fecha))
                return fecha;

            // Intentar parsear como número de serie de Excel
            if (int.TryParse(fechaStr, out int excelSerialDate))
            {
                var baseDate = new DateTime(1900, 1, 1);
                return baseDate.AddDays(excelSerialDate - 2); // Ajuste por bug de Excel
            }

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
            // Actualizar contadores por tipo
            var tipoKey = movimiento.Tipo.ToString();
            if (statistics.MovimientosPorTipo.ContainsKey(tipoKey))
                statistics.MovimientosPorTipo[tipoKey]++;
            else
                statistics.MovimientosPorTipo[tipoKey] = 1;

            // Actualizar fechas
            if (statistics.FechaMasAntigua == null || movimiento.FechaConcertacion < statistics.FechaMasAntigua)
                statistics.FechaMasAntigua = movimiento.FechaConcertacion;

            if (statistics.FechaMasReciente == null || movimiento.FechaConcertacion > statistics.FechaMasReciente)
                statistics.FechaMasReciente = movimiento.FechaConcertacion;
        }
    }
}