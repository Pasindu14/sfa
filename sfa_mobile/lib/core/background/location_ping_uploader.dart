import 'dart:async';

/// Rows per upload request. The API rejects more than 500 pings in one
/// request; 200 keeps each request small on a weak field connection.
const locationPingChunkSize = 200;

/// Drains the `pending_location_pings` outbox in fixed-size chunks.
///
/// Storage and transport are injected so the chunking contract is testable
/// without SQLite or Dio:
///  - oldest rows first,
///  - a chunk's rows are deleted only after its POST succeeded,
///  - the first failure stops the drain and rethrows, so chunks already
///    accepted are never resent and the rest wait for the next window.
class LocationPingUploader {
  LocationPingUploader({
    required Future<List<Map<String, Object?>>> Function(int limit) loadOldest,
    required Future<void> Function(List<Map<String, Object?>> pings) post,
    required Future<void> Function(List<int> ids) deleteIds,
    this.chunkSize = locationPingChunkSize,
    this.maxChunksPerFlush = 20,
  }) : _loadOldest = loadOldest,
       _post = post,
       _deleteIds = deleteIds;

  final Future<List<Map<String, Object?>>> Function(int limit) _loadOldest;
  final Future<void> Function(List<Map<String, Object?>> pings) _post;
  final Future<void> Function(List<int> ids) _deleteIds;
  final int chunkSize;

  /// Hard stop against an endless loop if deletes ever silently no-op. 20
  /// chunks of 200 is twice the 2000-row outbox cap.
  final int maxChunksPerFlush;

  /// Uploads until the outbox is empty, a chunk fails (rethrown), or
  /// [maxChunksPerFlush] is reached. Returns how many rows were uploaded.
  Future<int> flush() async {
    var uploaded = 0;
    for (var chunk = 0; chunk < maxChunksPerFlush; chunk++) {
      final rows = await _loadOldest(chunkSize);
      if (rows.isEmpty) break;

      await _post(rows.map(toPayload).toList());
      // Only delete rows that were successfully uploaded.
      await _deleteIds(rows.map((r) => r['id'] as int).toList());
      uploaded += rows.length;

      if (rows.length < chunkSize) break;
    }
    return uploaded;
  }

  /// DB row → API `PingItem`.
  static Map<String, Object?> toPayload(Map<String, Object?> r) => {
    'latitude': r['lat'],
    'longitude': r['lng'],
    'accuracy': r['accuracy'],
    'recordedAt': r['recorded_at'],
  };
}

/// Decides which 5-minute ticks also upload.
///
/// Positions are still captured every tick; uploading only every other tick
/// halves the radio wake-ups. The web live map marks a rep stale after 15
/// minutes without a ping, so the window must stay well under that.
class UploadCadence {
  UploadCadence({
    this.window = const Duration(minutes: 10),
    this.tickInterval = const Duration(minutes: 5),
  });

  final Duration window;
  final Duration tickInterval;
  DateTime? _lastAttempt;

  /// Due on the first tick, then once [window] has elapsed. Timer ticks drift
  /// by milliseconds, so a tick landing a hair short of the window must still
  /// count — otherwise the upload slips a whole extra tick (15 min). Half a
  /// tick of tolerance absorbs that without ever uploading on consecutive
  /// ticks.
  bool isDue(DateTime now) {
    final last = _lastAttempt;
    if (last == null) return true;
    final elapsed = now.difference(last);
    // A clock set backwards: don't wait out the negative gap.
    if (elapsed.isNegative) return true;
    return elapsed >= window - tickInterval ~/ 2;
  }

  /// Recorded on attempt, success or not: a failed upload retries at the next
  /// window rather than on every tick.
  void markAttempt(DateTime now) => _lastAttempt = now;
}
