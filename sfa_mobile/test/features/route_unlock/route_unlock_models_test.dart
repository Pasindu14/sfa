// The API sends every enum as its member name (global JsonStringEnumConverter).
// These guard that the models keep them as strings — a numeric map on the
// client makes every status comparison silently false — and that the many
// nullable fields parse from null without cast errors.
import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/features/route_unlock/data/models/route_unlock_models.dart';
import 'package:uswatte/features/route_unlock/domain/entities/route_unlock_request.dart';

Map<String, dynamic> requestJson({
  String status = 'Pending',
  String? effectiveStatus = 'Pending',
  bool isCurrentlyEffective = false,
  Map<String, dynamic> extra = const {},
}) =>
    {
      'id': 12,
      'userId': 7,
      'userName': 'Kamal Perera',
      'loginName': 'kamal',
      'routeId': 3,
      'routeName': 'Kandy Town A',
      'businessDate': '2026-09-30',
      'status': status,
      'effectiveStatus': effectiveStatus,
      'isCurrentlyEffective': isCurrentlyEffective,
      'requestReason': 'GPS not accurate',
      'requestedAt': '2026-09-30T03:10:00Z',
      'requestLatitude': null,
      'requestLongitude': null,
      'requestGpsAccuracyMeters': null,
      'supervisorUserId': null,
      'supervisorName': null,
      'reviewedByUserId': null,
      'reviewedByName': null,
      'reviewedByRole': null,
      'reviewedAt': null,
      'reviewNote': null,
      'validFrom': null,
      'validTo': null,
      'revokedByUserId': null,
      'revokedByName': null,
      'revokedAt': null,
      'revokeReason': null,
      'cancelledAt': null,
      'rowVersion': 123456,
      ...extra,
    };

void main() {
  group('RouteUnlockRequestModel.fromJson', () {
    test('parses a fresh pending request with every optional field null', () {
      final r = RouteUnlockRequestModel.fromJson(requestJson());

      expect(r.id, 12);
      expect(r.userName, 'Kamal Perera');
      expect(r.loginName, 'kamal');
      expect(r.routeName, 'Kandy Town A');
      expect(r.status, RouteUnlockStatus.pending);
      expect(r.effectiveStatus, 'Pending');
      expect(r.isPending, isTrue);
      expect(r.isLive, isFalse);
      expect(r.requestedAt, DateTime.utc(2026, 9, 30, 3, 10));
      expect(r.requestLatitude, isNull);
      expect(r.supervisorName, isNull);
      expect(r.validTo, isNull);
      expect(r.rowVersion, 123456);
    });

    test('keeps string enums and reads Expired from effectiveStatus', () {
      final r = RouteUnlockRequestModel.fromJson(
          requestJson(status: 'Pending', effectiveStatus: 'Expired'));

      expect(r.status, 'Pending');
      expect(r.effectiveStatus, 'Expired');
      expect(r.isPending, isFalse, reason: 'decisions key off effectiveStatus');
      expect(r.isExpired, isTrue);
    });

    test('falls back to status when effectiveStatus is absent', () {
      final json = requestJson(status: 'Rejected')..remove('effectiveStatus');
      final r = RouteUnlockRequestModel.fromJson(json);

      expect(r.effectiveStatus, 'Rejected');
      expect(r.isRejected, isTrue);
    });

    test('parses an approved, live request with numeric ints as doubles', () {
      final validTo = DateTime.now().toUtc().add(const Duration(hours: 5));
      final r = RouteUnlockRequestModel.fromJson(requestJson(
        status: 'Approved',
        effectiveStatus: 'Approved',
        isCurrentlyEffective: true,
        extra: {
          'requestLatitude': 7,
          'requestLongitude': 80.63,
          'requestGpsAccuracyMeters': 12,
          'reviewedByUserId': 4,
          'reviewedByName': 'Nimal S',
          'reviewedByRole': 'Supervisor',
          'validFrom': '2026-09-30T03:20:00Z',
          'validTo': validTo.toIso8601String(),
        },
      ));

      expect(r.requestLatitude, 7.0);
      expect(r.requestGpsAccuracyMeters, 12.0);
      expect(r.reviewedByRole, 'Supervisor');
      expect(r.isLive, isTrue);
    });

    test('an approved request past validTo is not live on the device', () {
      final r = RouteUnlockRequestModel.fromJson(requestJson(
        status: 'Approved',
        effectiveStatus: 'Approved',
        isCurrentlyEffective: true,
        extra: {'validTo': '2020-01-01T00:00:00Z'},
      ));

      expect(r.isLive, isFalse);
    });
  });

  group('RouteUnlockDetailModel.fromJson', () {
    test('parses events oldest-first and bills with nullable distance', () {
      final d = RouteUnlockDetailModel.fromJson({
        'request': requestJson(status: 'Approved', effectiveStatus: 'Approved'),
        'events': [
          {
            'id': 2,
            'action': 'Approved',
            'fromStatus': 'Pending',
            'toStatus': 'Approved',
            'performedByUserId': 4,
            'performedByName': 'Nimal S',
            'performedByRole': 'Supervisor',
            'performedAt': '2026-09-30T03:20:00Z',
            'note': null,
            'ipAddress': null,
          },
          {
            'id': 1,
            'action': 'Requested',
            'fromStatus': null,
            'toStatus': 'Pending',
            'performedByUserId': 7,
            'performedByName': null,
            'performedByRole': 'SalesRep',
            'performedAt': '2026-09-30T03:10:00Z',
          },
        ],
        'bills': [
          {
            'billingId': 99,
            'billingNumber': 'B-0099',
            'billingDate': '2026-09-30T05:00:00Z',
            'outletId': 5,
            'outletName': 'Lucky Stores',
            'distanceFromOutletMeters': null,
            'totalAmount': 1500,
            'createdAt': '2026-09-30T05:00:00Z',
          },
        ],
      });

      expect(d.events.map((e) => e.action), ['Requested', 'Approved']);
      expect(d.events.first.fromStatus, isNull);
      expect(d.events.first.performedByName, isNull);
      expect(d.bills.single.billingNumber, 'B-0099');
      expect(d.bills.single.distanceFromOutletMeters, isNull);
      expect(d.bills.single.totalAmount, 1500.0);
    });

    test('tolerates missing events and bills arrays', () {
      final d = RouteUnlockDetailModel.fromJson({'request': requestJson()});

      expect(d.events, isEmpty);
      expect(d.bills, isEmpty);
    });
  });
}
