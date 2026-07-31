using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace sfa_api.Common.Errors;

/// <summary>
/// Turns model-binding failures into messages that are safe to show a user.
/// System.Text.Json writes deserialization errors like
/// "The JSON value could not be converted to sfa_api.Features.SalesInvoices.Requests.ImportSalesInvoiceRequest.
/// Path: $.invoices[1].invoiceDate | LineNumber: 0 | BytePositionInLine: 1987."
/// — internal type names and byte offsets that mean nothing to the caller and leak our
/// internals (see .claude/rules/never-do.md). Only the JSON path is actionable, so that
/// is all we keep.
/// </summary>
public static class ModelErrorFormatter
{
    public static string Describe(ModelError error)
    {
        if (error.Exception is JsonException json)
            return DescribeJsonPath(json.Path);

        return string.IsNullOrEmpty(error.ErrorMessage) ? "Invalid value." : error.ErrorMessage;
    }

    private static string DescribeJsonPath(string? path)
    {
        if (string.IsNullOrEmpty(path) || path == "$")
            return "The request body is not valid JSON.";

        // "$.invoices[1].invoiceDate" → "invoices[1].invoiceDate"
        var field = path.StartsWith("$.", StringComparison.Ordinal) ? path[2..] : path.TrimStart('$');
        return $"Invalid value for '{field}'.";
    }
}
