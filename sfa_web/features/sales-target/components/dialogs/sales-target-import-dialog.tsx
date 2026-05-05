'use client'

import { useRef, useState } from 'react'
import {
  Upload, ArrowLeft,
  CheckCircle2, AlertTriangle, XCircle,
  TrendingUp, RefreshCcw, SkipForward, Hash,
} from 'lucide-react'
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { ScrollArea } from '@/components/ui/scroll-area'
import { parseTargetsExcel } from '../../lib/parse-targets-excel'
import { useImportTargetDialog } from '../../store/sales-target-dialog.store'
import { useImportSalesTargets } from '../../hooks/sales-target.hooks'
import type { ImportSalesTargetsPayload, ImportSalesTargetsResult } from '../../schema/sales-target.schema'

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
    : isPartial
      ? 'Import completed with skipped rows'
      : 'Import failed'

  return (
    <div className="space-y-4">
      <div className={`flex items-center gap-3 rounded-lg border px-4 py-3 ${statusBg}`}>
        <StatusIcon className={`h-5 w-5 shrink-0 ${statusColor}`} />
        <div className="min-w-0 flex-1">
          <p className={`text-sm font-semibold ${statusColor}`}>{statusLabel}</p>
          <p className="text-xs text-muted-foreground">
            Batch{' '}
            <code className="rounded bg-background/60 px-1.5 py-0.5 font-mono text-xs">
              {result.batchNumber}
            </code>
            {' '}— {MONTH_LABELS[result.month]} {result.year}
          </p>
        </div>
      </div>

      <div className="grid grid-cols-4 gap-3">
        {[
          { label: 'Inserted', value: result.insertedRows, icon: TrendingUp, color: 'bg-green-100 text-green-600' },
          { label: 'Updated', value: result.updatedRows, icon: RefreshCcw, color: 'bg-blue-100 text-blue-600' },
          { label: 'Skipped', value: result.skippedRows, icon: SkipForward, color: result.skippedRows > 0 ? 'bg-amber-100 text-amber-600' : 'bg-muted text-muted-foreground' },
          { label: 'Total', value: result.totalRows, icon: Hash, color: 'bg-violet-100 text-violet-600' },
        ].map(({ label, value, icon: Icon, color }) => (
          <div key={label} className="flex items-center gap-3 rounded-lg border bg-card p-3">
            <div className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-md ${color}`}>
              <Icon className="h-4 w-4" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">{label}</p>
              <p className={`text-xl font-bold leading-tight ${value > 0 && label === 'Skipped' ? 'text-amber-600' : ''}`}>
                {value}
              </p>
            </div>
          </div>
        ))}
      </div>

      {result.errors.length > 0 && (
        <div className="rounded-lg border border-destructive/20 overflow-hidden">
          <div className="flex items-center gap-2 border-b border-destructive/20 bg-destructive/5 px-3 py-2">
            <XCircle className="h-3.5 w-3.5 shrink-0 text-destructive" />
            <span className="text-xs font-semibold text-destructive">Skipped Rows</span>
            <span className="ml-auto rounded-full bg-destructive/10 px-2 py-0.5 text-xs font-medium text-destructive">
              {result.errors.length}
            </span>
          </div>
          <div className="grid grid-cols-[3rem_5rem_8rem_1fr] gap-x-3 border-b bg-muted/50 px-3 py-1.5 text-xs font-medium text-muted-foreground">
            <span>Row</span>
            <span>Rep Code</span>
            <span>Item Code</span>
            <span>Reason</span>
          </div>
          <ScrollArea className="h-64">
            <div className="divide-y divide-border/60">
              {result.errors.map((err, i) => (
                <div key={i} className="grid grid-cols-[3rem_5rem_8rem_1fr] items-start gap-x-3 px-3 py-2 text-sm hover:bg-muted/30">
                  <span className="text-xs tabular-nums text-muted-foreground/60 pt-0.5">{err.rowIndex}</span>
                  <code className="font-mono text-xs">{err.repsCode}</code>
                  <code className="font-mono text-xs truncate">{err.itemCode}</code>
                  <span className="text-xs text-muted-foreground leading-relaxed">{err.reason}</span>
                </div>
              ))}
            </div>
          </ScrollArea>
        </div>
      )}

      <DialogFooter>
        <Button onClick={onClose} className="min-w-24">Done</Button>
      </DialogFooter>
    </div>
  )
}

// ── Preview view ──────────────────────────────────────────────────────────

function PreviewView({
  payload,
  onBack,
  onConfirm,
  isPending,
}: {
  payload: ImportSalesTargetsPayload
  onBack: () => void
  onConfirm: () => void
  isPending: boolean
}) {
  const preview = payload.rows.slice(0, 50)
  return (
    <div className="flex min-h-0 flex-1 flex-col gap-4">
      <div className="flex items-center gap-3 rounded-lg border bg-muted/40 px-4 py-2.5">
        <div className="text-sm font-medium">
          {MONTH_LABELS[payload.month]} {payload.year}
        </div>
        <div className="ml-auto text-sm text-muted-foreground">
          <span className="font-semibold text-foreground">{payload.rows.length.toLocaleString()}</span> rows ready to import
        </div>
      </div>

      <div className="min-h-0 flex-1 overflow-hidden rounded-lg border">
        <div className="grid grid-cols-[3rem_6rem_10rem_1fr] gap-x-3 border-b bg-muted/50 px-3 py-2 text-xs font-medium text-muted-foreground sticky top-0">
          <span>Row</span>
          <span>Rep Code</span>
          <span>Item Code</span>
          <span>Target Qty</span>
        </div>
        <ScrollArea className="h-64">
          <div className="divide-y divide-border/60">
            {preview.map((row) => (
              <div key={row.rowIndex} className="grid grid-cols-[3rem_6rem_10rem_1fr] items-center gap-x-3 px-3 py-1.5 text-sm hover:bg-muted/30">
                <span className="text-xs tabular-nums text-muted-foreground/60">{row.rowIndex}</span>
                <code className="font-mono text-xs">{row.repsCode}</code>
                <code className="font-mono text-xs truncate">{row.itemCode}</code>
                <span className="tabular-nums">{row.targetQty.toLocaleString()}</span>
              </div>
            ))}
          </div>
        </ScrollArea>
      </div>
      {payload.rows.length > 50 && (
        <p className="text-xs text-center text-muted-foreground">
          Showing first 50 of {payload.rows.length.toLocaleString()} rows
        </p>
      )}

      <DialogFooter className="shrink-0 border-t pt-4">
        <Button variant="outline" onClick={onBack} disabled={isPending}>Back</Button>
        <Button onClick={onConfirm} disabled={isPending}>
          {isPending ? (
            <><Spinner className="mr-2" />Importing…</>
          ) : (
            `Import ${payload.rows.length.toLocaleString()} Rows`
          )}
        </Button>
      </DialogFooter>
    </div>
  )
}

// ── File picker view ──────────────────────────────────────────────────────

function FilePicker({ onParsed }: { onParsed: (p: ImportSalesTargetsPayload) => void }) {
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
      const payload = parseTargetsExcel(buffer, file.name)
      if (payload.rows.length === 0) {
        setParseError('No data rows found — check the file format.')
        return
      }
      onParsed(payload)
    } catch (err) {
      setParseError(err instanceof Error ? err.message : 'Failed to parse file.')
    } finally {
      setIsParsing(false)
    }
  }

  return (
    <div className="space-y-4">
      <div
        className="flex cursor-pointer flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed border-muted-foreground/25 p-12 text-center transition-colors hover:border-muted-foreground/50 hover:bg-muted/30"
        onClick={() => inputRef.current?.click()}
      >
        <Upload className="h-8 w-8 text-muted-foreground/50" />
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
            <p className="font-medium">{fileName}</p>
            <p className="text-sm text-muted-foreground">Click to change file</p>
          </>
        ) : (
          <>
            <p className="font-medium">Click to select file</p>
            <p className="text-sm text-muted-foreground">Sales targets Excel export (.xlsx)</p>
          </>
        )}
      </div>

      {parseError && (
        <p className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">{parseError}</p>
      )}

      <DialogFooter>
        <Button onClick={handlePreview} disabled={!fileName || isParsing}>
          {isParsing ? (<><Spinner className="mr-2" />Parsing…</>) : 'Preview Data'}
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
  const [payload, setPayload] = useState<ImportSalesTargetsPayload | null>(null)

  function handleParsed(p: ImportSalesTargetsPayload) {
    setPayload(p)
    setView('preview')
  }

  function handleConfirm() {
    if (!payload) return
    mutate(payload, { onSuccess: () => setView('result') })
  }

  function handleOpenChange(open: boolean) {
    if (!open && !isPending) {
      closeAndReset()
      setView('picker')
      setPayload(null)
    }
  }

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
      <DialogContent className="w-[80vw]! max-w-[96vw]! h-[75vh]! max-h-[92vh]! flex flex-col">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            {view === 'preview' && (
              <button
                onClick={() => setView('picker')}
                className="text-muted-foreground hover:text-foreground"
                disabled={isPending}
              >
                <ArrowLeft className="h-4 w-4" />
              </button>
            )}
            {titles[view]}
          </DialogTitle>
          <DialogDescription>{descriptions[view]}</DialogDescription>
        </DialogHeader>

        {view === 'picker' && <FilePicker onParsed={handleParsed} />}

        {view === 'preview' && payload && (
          <PreviewView
            payload={payload}
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
              setPayload(null)
            }}
          />
        )}
      </DialogContent>
    </Dialog>
  )
}
