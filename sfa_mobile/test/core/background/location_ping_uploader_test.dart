import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/core/background/location_ping_uploader.dart';

/// In-memory `pending_location_pings` + a scripted API.
class _Outbox {
  _Outbox(int count) {
    for (var i = 1; i <= count; i++) {
      rows.add({
        'id': i,
        'lat': 6.9,
        'lng': 79.8,
        'accuracy': 12.0,
        'recorded_at': 'ping-$i',
        'created_at': '2026-09-17T00:00:00Z',
      });
    }
  }

  final rows = <Map<String, Object?>>[];

  /// Recorded batches, in order: the `recordedAt` of each ping.
  final posted = <List<Object?>>[];

  /// 1-based POST numbers that fail.
  final failOnPost = <int>{};
  int _posts = 0;

  LocationPingUploader uploader() => LocationPingUploader(
    loadOldest: (limit) async => rows.take(limit).toList(),
    post: (pings) async {
      _posts++;
      if (failOnPost.contains(_posts)) throw Exception('network down');
      posted.add(pings.map((p) => p['recordedAt']).toList());
    },
    deleteIds: (ids) async => rows.removeWhere((r) => ids.contains(r['id'])),
  );
}

void main() {
  group('LocationPingUploader', () {
    test('empty outbox makes no request', () async {
      final box = _Outbox(0);
      expect(await box.uploader().flush(), 0);
      expect(box.posted, isEmpty);
    });

    test('uploads oldest first in chunks of 200 until empty', () async {
      final box = _Outbox(450);
      expect(await box.uploader().flush(), 450);
      expect(box.posted.map((b) => b.length), [200, 200, 50]);
      expect(box.posted.first.first, 'ping-1');
      expect(box.rows, isEmpty);
    });

    test(
      'an exact multiple of the chunk size needs no extra request',
      () async {
        final box = _Outbox(400);
        await box.uploader().flush();
        expect(box.posted.map((b) => b.length), [200, 200]);
        expect(box.rows, isEmpty);
      },
    );

    test('maps DB columns to the API ping shape', () {
      expect(
        LocationPingUploader.toPayload({
          'id': 1,
          'lat': 1.5,
          'lng': 2.5,
          'accuracy': 9.0,
          'recorded_at': 'T',
          'created_at': 'C',
        }),
        {'latitude': 1.5, 'longitude': 2.5, 'accuracy': 9.0, 'recordedAt': 'T'},
      );
    });

    test('partial failure keeps the accepted chunk deleted, stops, and the '
        'next window resends only what was not accepted', () async {
      final box = _Outbox(450)..failOnPost.add(2);

      await expectLater(box.uploader().flush(), throwsException);
      // Chunk 1 accepted and deleted; chunk 2 failed; chunk 3 never tried.
      expect(box.posted.map((b) => b.length), [200]);
      expect(box.rows.length, 250);
      expect(box.rows.first['id'], 201);

      // Next window succeeds: ids 1-200 are never sent again.
      final firstBatch = box.posted.first.toSet();
      await box.uploader().flush();
      expect(box.posted.map((b) => b.length), [200, 200, 50]);
      for (final batch in box.posted.skip(1)) {
        expect(batch.toSet().intersection(firstBatch), isEmpty);
      }
      expect(box.rows, isEmpty);
    });

    test(
      'a failing delete stops the drain instead of re-posting the chunk',
      () async {
        final box = _Outbox(300);
        final posts = <int>[];
        final uploader = LocationPingUploader(
          loadOldest: (limit) async => box.rows.take(limit).toList(),
          post: (pings) async => posts.add(pings.length),
          deleteIds: (_) async => throw Exception('disk full'),
        );
        await expectLater(uploader.flush(), throwsException);
        expect(posts, [200]);
      },
    );

    test('stops after maxChunksPerFlush even if rows never drain', () async {
      final box = _Outbox(200);
      var posts = 0;
      final uploader = LocationPingUploader(
        loadOldest: (limit) async => box.rows.take(limit).toList(),
        post: (_) async => posts++,
        deleteIds: (_) async {}, // silently deletes nothing
        maxChunksPerFlush: 3,
      );
      expect(await uploader.flush(), 600);
      expect(posts, 3);
    });
  });

  group('UploadCadence', () {
    final t0 = DateTime.utc(2026, 9, 17, 8);

    test('first tick uploads', () {
      expect(UploadCadence().isDue(t0), isTrue);
    });

    test('the next 5-minute tick does not, the one after does', () {
      final c = UploadCadence()..markAttempt(t0);
      expect(c.isDue(t0.add(const Duration(minutes: 5))), isFalse);
      expect(c.isDue(t0.add(const Duration(minutes: 10))), isTrue);
    });

    test('timer drift a hair short of the window still uploads', () {
      final c = UploadCadence()..markAttempt(t0);
      expect(
        c.isDue(
          t0.add(const Duration(minutes: 9, seconds: 59, milliseconds: 950)),
        ),
        isTrue,
      );
    });

    test('a clock set backwards does not stall uploads', () {
      final c = UploadCadence()..markAttempt(t0);
      expect(c.isDue(t0.subtract(const Duration(hours: 1))), isTrue);
    });

    test(
      'over a day of drifting 5-minute ticks, uploads never run on '
      'consecutive ticks and never leave more than 10 minutes between them',
      () {
        final c = UploadCadence();
        var now = t0;
        DateTime? lastUpload;
        var lastTickUploaded = false;
        var uploads = 0;
        for (var tick = 0; tick < 288; tick++) {
          // Alternate early/late jitter of up to 200ms.
          now = now.add(
            Duration(minutes: 5, milliseconds: tick.isEven ? -200 : 150),
          );
          final due = c.isDue(now);
          if (due) {
            c.markAttempt(now);
            expect(lastTickUploaded, isFalse, reason: 'consecutive at $tick');
            if (lastUpload != null) {
              expect(
                now.difference(lastUpload),
                lessThanOrEqualTo(const Duration(minutes: 10, seconds: 1)),
              );
            }
            lastUpload = now;
            uploads++;
          }
          lastTickUploaded = due;
        }
        expect(uploads, 144);
      },
    );
  });
}
