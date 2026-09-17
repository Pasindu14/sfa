// T3.24: the memoized search filter returns exactly what the per-build
// `where(... toLowerCase().contains ...)` filter did, in the same order.
import 'dart:math';

import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/core/utils/search_filter_cache.dart';

typedef _Row = ({String code, String name});

List<_Row> _old(List<_Row> items, String query) {
  if (query.isEmpty) return items;
  final q = query.toLowerCase();
  return items
      .where((s) =>
          s.code.toLowerCase().contains(q) || s.name.toLowerCase().contains(q))
      .toList();
}

void main() {
  test('matches the old filter for random data and queries', () {
    final rnd = Random(7);
    const alphabet = 'aAbBcC1 -ÉéİiSsß';
    String word(int n) =>
        String.fromCharCodes([for (var i = 0; i < n; i++) alphabet.codeUnitAt(rnd.nextInt(alphabet.length))]);

    for (var round = 0; round < 200; round++) {
      final items = [
        for (var i = 0; i < rnd.nextInt(30); i++)
          (code: word(rnd.nextInt(6)), name: word(rnd.nextInt(10))),
      ];
      final cache = SearchFilterCache<_Row>((r) => [r.code, r.name]);
      for (var q = 0; q < 5; q++) {
        final query = word(rnd.nextInt(3));
        expect(cache.apply(items, query), _old(items, query));
      }
    }
  });

  test('empty query returns the source list itself', () {
    final items = [(code: 'A1', name: 'Soap')];
    final cache = SearchFilterCache<_Row>((r) => [r.code, r.name]);
    expect(identical(cache.apply(items, ''), items), isTrue);
  });

  test('reuses the result until the list identity or query changes', () {
    var fieldCalls = 0;
    final cache = SearchFilterCache<_Row>((r) {
      fieldCalls++;
      return [r.code, r.name];
    });
    final items = [(code: 'A1', name: 'Soap'), (code: 'B2', name: 'Shampoo')];

    final first = cache.apply(items, 'S');
    expect(identical(cache.apply(items, 'S'), first), isTrue);
    expect(fieldCalls, 2);

    expect(cache.apply(items, 'sha'), [items[1]]);
    expect(fieldCalls, 2); // same list: lowercased fields not recomputed

    final reloaded = [...items, (code: 'C3', name: 'Salt')];
    expect(cache.apply(reloaded, 'sha'), [items[1]]);
    expect(cache.apply(reloaded, 's'), reloaded);
    expect(fieldCalls, 5);
  });

  test('a field boundary is never matched across code and name', () {
    final items = [(code: 'AB', name: 'CD')];
    final cache = SearchFilterCache<_Row>((r) => [r.code, r.name]);
    expect(cache.apply(items, 'bc'), isEmpty);
  });
}
