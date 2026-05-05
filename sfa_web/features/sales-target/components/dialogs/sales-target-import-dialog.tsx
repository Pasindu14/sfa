'use client'

import { useRef, useState } from 'react'
import {
  Upload, ArrowLeft,
  CheckCircle2, AlertTriangle, XCircle,
  TrendingUp, RefreshCcw, SkipForward, Hash,
  FileSpreadsheet,
} from 'lucide-react'
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Badge } from '@/components/ui/badge'
import { parseTargetsExcel, toApiPayload } from '../../lib/parse-targets-excel'
import { useImportTargetDialog } from '../../store/sales-target-dialog.store'
import { useImportSalesTargets } from '../../hooks/sales-target.hooks'
import type { ParsedTargetsData } from '../../lib/parse-targets-excel'
import type { ImportSalesTargetsResult } from '../../schema/sales-target.schema'

type View = 'picker' | 'preview' | 'result'

const MONTH_LABELS = [
  '', 'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

// ── Result view ───────────────────────────────────────────────────────────

function ResultView({ result, onClose }: { result: ImportSalesTargetsResult; onClose: () => void }) {
  const isCompleted = result.status === 'Completed'
  const isPartial = result.status === 'PartialFailed'
  const StatusIcon = isCompleted ? CheckCircle2 : isPartial ? AlertTriangle : XCircle
  const statusColor = isCompleted ? 'text-green-600' : isPartial ? 'text-amber-600' : 'text-destructive'
  const statusBg = isCompleted
    ? 'bg-green-50 border-green-200'
    : isPartial
      ? 'bg-amber-50 border-amber-200'
      : 'bg-destructive/5 border-destructive/20'
  const statusLabel = isCompleted
    ? 'All targets imported successfully'
    : isPartial ? 'Import completed with skipped rows'
      : 'Import failed'

  return (
    <div className="space-y-3">
      <div className={`flex items-center gap-3 rounded-lg border px-3 py-2.5 ${statusBg}`}>
        <StatusIcon className={`h-4 w-4 shrink-0 ${statusColor}`} />
        <div className="min-w-0 flex-1">
          <p className={`text-sm font-semibold ${statusColor}`}>{statusLabel}</p>
          <p className="text-xs text-muted-foreground">
            Batch{' '}
            <code className="rounded bg-background/60 px-1 py-0.5 font-mono text-xs">{result.batchNumber}</code>
            {' '}— {MONTH_LABELS[result.month]} {result.year}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-4 gap-2">
        {[
          { label: 'Inserted', value: result.insertedRows, icon: TrendingUp, color: 'bg-green-100 text-green-600' },
          { label: 'Updated', value: result.updatedRows, icon: RefreshCcw, color: 'bg-blue-100 text-blue-600' },
          { label: 'Skipped', value: result.skippedRows, icon: SkipForward, color: result.skippedRows > 0 ? 'bg-amber-100 text-amber-600' : 'bg-muted text-muted-foreground' },
          { label: 'Total', value: result.totalRows, icon: Hash, color: 'bg-violet-100 text-violet-600' },
        ].map(({ label, value, icon: Icon, color }) => (
          <div key={label} className="flex items-center gap-2 rounded-lg border bg-card p-2.5">
            <div className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-md ${color}`}>
              <Icon className="h-3.5 w-3.5" />
            </div>
            <div>
              <p className="text-[10px] text-muted-foreground leading-none mb-0.5">{label}</p>
              <p className="text-lg font-bold leading-none">{value.toLocaleString()}</p>
            </div>
          </div>
        ))}
      </div>

      {result.errors.length > 0 && (
        <div className="rounded-lg border border-destructive/20 overflow-hidden">
          <div className="flex items-center gap-2 border-b border-destructive/20 bg-destructive/5 px-3 py-1.5">
            <XCircle className="h-3 w-3 shrink-0 text-destructive" />
            <span className="text-xs font-semibold text-destructive">Skipped Rows</span>
            <Badge variant="destructive" className="ml-auto h-4 text-[10px] px-1.5">{result.errors.length}</Badge>
          </div>
          <div className="grid grid-cols-[3rem_5rem_8rem_1fr] gap-x-3 border-b bg-muted/40 px-3 py-1 text-[10px] font-medium uppercase tracking-wider text-muted-foreground">
            <span>Row</span><span>Rep</span><span>Item</span><span>Reason</span>
          </div>
          <ScrollArea className="h-44">
            <div className="divide-y divide-border/50">
              {result.errors.map((err, i) => (
                <div key={i} className="grid grid-cols-[3rem_5rem_8rem_1fr] items-start gap-x-3 px-3 py-1.5 text-xs hover:bg-muted/30">
                  <span className="tabular-nums text-muted-foreground/60">{err.rowIndex}</span>
                  <code className="font-mono">{err.repsCode}</code>
                  <code className="font-mono truncate">{err.itemCode}</code>
                  <span className="text-muted-foreground leading-relaxed">{err.reason}</span>
                </div>
              ))}
            </div>
          </ScrollArea>
        </div>
      )}

      <DialogFooter className="pt-1">
        <Button onClick={onClose} size="sm">Done</Button>
      </DialogFooter>
    </div>
  )
}

// ── Preview view ──────────────────────────────────────────────────────────

function PreviewView({
  data, onBack, onConfirm, isPending,
}: {
  data: ParsedTargetsData
  onBack: () => void
  onConfirm: () => void
  isPending: boolean
}) {
  const preview = data.rows.slice(0, 100)

  return (
    <div className="flex min-h-0 flex-1 flex-col gap-3">
      {/* Period + count banner */}
      <div className="flex items-center gap-3 rounded-lg border bg-muted/30 px-3 py-2">
        <FileSpreadsheet className="h-4 w-4 shrink-0 text-muted-foreground" />
        <span className="text-sm font-medium">{MONTH_LABELS[data.month]} {data.year}</span>
        <span className="text-xs text-muted-foreground">{data.fileName}</span>
        <div className="ml-auto">
          <Badge variant="secondary" className="tabular-nums">
            {data.rows.length.toLocaleString()} rows
          </Badge>
        </div>
      </div>

      {/* Table */}
      <div className="min-h-0 flex-1 overflow-hidden rounded-lg border">
        {/* Header */}
        <div className="grid grid-cols-[3.5rem_1fr_1fr_1fr_5rem] gap-x-2 border-b bg-muted/50 px-3 py-1.5 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
          <span>Row</span>
          <span>Rep Code · Name</span>
          <span>Item Code</span>
          <span>Item Name</span>
          <span className="text-right">Target Qty</span>
        </div>
        <ScrollArea className="h-[280px]">
          <div className="divide-y divide-border/40">
            {preview.map((row) => (
              <div
                key={row.rowIndex}
                className="grid grid-cols-[3.5rem_1fr_1fr_1fr_5rem] items-center gap-x-2 px-3 py-1.5 text-xs hover:bg-muted/20 transition-colors"
              >
                <span className="tabular-nums text-muted-foreground/50 text-[10px]">{row.rowIndex}</span>
                <div className="min-w-0">
                  <span className="font-mono font-medium text-[11px]">{row.repsCode}</span>
                  {row.repName && (
                    <span className="ml-1.5 text-muted-foreground truncate">{row.repName}</span>
                  )}
                </div>
                <code className="font-mono text-[11px] truncate">{row.itemCode}</code>
                <span className="text-muted-foreground truncate">{row.itemName}</span>
                <span className="tabular-nums text-right font-semibold">{row.targetQty.toLocaleString()}</span>
              </div>
            ))}
          </div>
        </ScrollArea>
      </div>

      {data.rows.length > 100 && (
        <p className="text-center text-[11px] text-muted-foreground">
          Showing first 100 of {data.rows.length.toLocaleString()} rows
        </p>
      )}

      <DialogFooter className="shrink-0 border-t pt-3">
        <Button variant="outline" size="sm" onClick={onBack} disabled={isPending}>Back</Button>
        <Button size="sm" onClick={onConfirm} disabled={isPending}>
          {isPending ? (
            <><Spinner className="mr-1.5 h-3 w-3" />Importing…</>
          ) : (
            `Import ${data.rows.length.toLocaleString()} Rows`
          )}
        </Button>
      </DialogFooter>
    </div>
  )
}

// ── File picker view ──────────────────────────────────────────────────────

function FilePicker({ onParsed }: { onParsed: (d: ParsedTargetsData) => void }) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [fileName, setFileName] = useState<string | null>(null)
  const [parseError, setParseError] = useState<string | null>(null)
  const [isParsing, setIsParsing] = useState(false)

  async function handlePreview() {
    const file = inputRef.current?.files?.[0]
    if (!file) return
    setIsParsing(true)
    setParseError(null)
    try {
      const buffer = await file.arrayBuffer()
      const data = parseTargetsExcel(buffer, file.name)
      if (data.rows.length === 0) {
        setParseError('No data rows found — check the file format.')
        return
      }
      onParsed(data)
    } catch (err) {
      setParseError(err instanceof Error ? err.message : 'Failed to parse file.')
    } finally {
      setIsParsing(false)
    }
  }

  return (
    <div className="space-y-3">
      <div
        className="flex cursor-pointer flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed border-muted-foreground/20 p-8 text-center transition-colors hover:border-primary/40 hover:bg-muted/20"
        onClick={() => inputRef.current?.click()}
      >
        <div className="flex h-10 w-10 items-center justify-center rounded-full bg-muted">
          <Upload className="h-4 w-4 text-muted-foreground" />
        </div>
        <input
          ref={inputRef}
          type="file"
          accept=".xlsx"
          className="hidden"
          onChange={(e) => {
            setFileName(e.target.files?.[0]?.name ?? null)
            setParseError(null)
          }}
        />
        {fileName ? (
          <>
            <p className="text-sm font-medium">{fileName}</p>
            <p className="text-xs text-muted-foreground">Click to change file</p>
          </>
        ) : (
          <>
            <p className="text-sm font-medium">Click to select file</p>
            <p className="text-xs text-muted-foreground">Sales targets Excel export (.xlsx)</p>
          </>
        )}
      </div>

      {parseError && (
        <p className="rounded-md bg-destructive/10 px-3 py-2 text-xs text-destructive">{parseError}</p>
      )}

      <DialogFooter>
        <Button size="sm" onClick={handlePreview} disabled={!fileName || isParsing}>
          {isParsing ? (<><Spinner className="mr-1.5 h-3 w-3" />Parsing…</>) : 'Preview Data'}
        </Button>
      </DialogFooter>
    </div>
  )
}

// ── Main dialog ───────────────────────────────────────────────────────────

export function SalesTargetImportDialog() {
  const { isOpen, close } = useImportTargetDialog()
  const { mutate, isPending, importResult, closeAndReset } = useImportSalesTargets()
  const [view, setView] = useState<View>('picker')
  const [parsed, setParsed] = useState<ParsedTargetsData | null>(null)

  function handleParsed(d: ParsedTargetsData) {
    setParsed(d)
    setView('preview')
  }

  function handleConfirm() {
    if (!parsed) return
    mutate(toApiPayload(parsed), { onSuccess: () => setView('result') })
  }

  function handleOpenChange(open: boolean) {
    if (!open && !isPending) {
      closeAndReset()
      setView('picker')
      setParsed(null)
    }
  }

  const isWide = view === 'preview' || view === 'result'

  const titles: Record<View, string> = {
    picker: 'Import Sales Targets',
    preview: 'Preview Data',
    result: 'Import Complete',
  }
  const descriptions: Record<View, string> = {
    picker: 'Upload the monthly targets Excel file (.xlsx).',
    preview: 'Review the data before importing.',
    result: 'Review the results of your import batch.',
  }

  return (
    <Dialog open={isOpen} onOpenChange={handleOpenChange}>
      <DialogContent
        className={`flex flex-col transition-all duration-200 ${
          isWide
            ? 'sm:max-w-3xl max-h-[88vh]'
            : 'sm:max-w-md'
        }`}
      >
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2 text-base">
            {view === 'preview' && (
              <button
                onClick={() => setView('picker')}
                className="rounded p-0.5 text-muted-foreground hover:text-foreground hover:bg-muted transition-colors"
                disabled={isPending}
              >
                <ArrowLeft className="h-4 w-4" />
              </button>
            )}
            {titles[view]}
          </DialogTitle>
          <DialogDescription className="text-xs">{descriptions[view]}</DialogDescription>
        </DialogHeader>

        {view === 'picker' && <FilePicker onParsed={handleParsed} />}

        {view === 'preview' && parsed && (
          <PreviewView
            data={parsed}
            onBack={() => setView('picker')}
            onConfirm={handleConfirm}
            isPending={isPending}
          />
        )}

        {view === 'result' && importResult && (
          <ResultView
            result={importResult}
            onClose={() => {
              closeAndReset()
              setView('picker')
              setParsed(null)
            }}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}
