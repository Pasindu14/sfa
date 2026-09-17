// On-demand loaders for the Excel libraries.
//
// exceljs (~900 KB) and xlsx (~330 KB) are only needed when a user actually exports or
// imports a spreadsheet, so they are pulled in with a dynamic import() at that moment
// instead of being bundled into every DataTable page. The module is cached by the bundler
// after the first load, so repeat exports pay nothing extra.

type ExcelJSModule = typeof import('exceljs')

/**
 * Load exceljs. It ships as CommonJS (`module.exports = { Workbook, ... }`), so depending on
 * the bundler's interop the classes sit either on the namespace itself or on `.default`.
 * Prefer `.default` (what `import ExcelJS from 'exceljs'` resolved to) and fall back to the
 * namespace.
 */
export async function loadExcelJS(): Promise<ExcelJSModule> {
  const mod = (await import('exceljs')) as ExcelJSModule & { default?: ExcelJSModule }
  return mod.default?.Workbook ? mod.default : mod
}

type XlsxModule = typeof import('xlsx')

/**
 * Load xlsx (SheetJS). The package exposes an ESM build with named exports, which is exactly
 * what `import * as XLSX from 'xlsx'` produced — so the namespace is used as-is, falling back
 * to `.default` only if a CommonJS build was resolved instead.
 */
export async function loadXlsx(): Promise<XlsxModule> {
  const mod = (await import('xlsx')) as XlsxModule & { default?: XlsxModule }
  return typeof mod.read === 'function' ? mod : (mod.default as XlsxModule)
}
