/// Lifecycle of a locally-created bill row.
///
/// - [pending]: awaiting its first sync attempt (or queued for retry).
/// - [syncing]: currently POSTing to the server.
/// - [synced]: server returned 2xx; server_bill_id + server_bill_number populated.
/// - [failed]: server returned 4xx (validation, stock-out, etc.); needs rep action.
enum SyncStatus { pending, syncing, synced, failed, cancelled }

extension SyncStatusX on SyncStatus {
  String get dbValue => switch (this) {
        SyncStatus.pending   => 'pending',
        SyncStatus.syncing   => 'syncing',
        SyncStatus.synced    => 'synced',
        SyncStatus.failed    => 'failed',
        SyncStatus.cancelled => 'cancelled',
      };

  static SyncStatus fromDb(String value) => switch (value) {
        'pending'   => SyncStatus.pending,
        'syncing'   => SyncStatus.syncing,
        'synced'    => SyncStatus.synced,
        'failed'    => SyncStatus.failed,
        'cancelled' => SyncStatus.cancelled,
        _           => SyncStatus.pending,
      };
}
