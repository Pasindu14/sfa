/// Memoizes a case-insensitive "any field contains the query" filter.
///
/// Built for list pages that re-run their search filter on every rebuild. The
/// lowercased search fields are computed once per source list, and the filtered
/// result is reused while both the source list (by identity) and the query stay
/// the same.
///
/// The result is identical to filtering on each build with
/// `fields(item).any((f) => f.toLowerCase().contains(query.toLowerCase()))`,
/// in source order. An empty query returns [source] itself.
///
/// Source lists must be treated as immutable: a list mutated in place keeps its
/// identity, so the cache would not notice the change.
class SearchFilterCache<T> {
  SearchFilterCache(this._fields);

  final List<String> Function(T item) _fields;

  List<T>? _indexedSource;
  List<List<String>> _lowerFields = const [];

  List<T>? _resultSource;
  String? _resultQuery;
  List<T> _result = const [];

  /// Returns the items of [source] with at least one field containing [query],
  /// ignoring case.
  List<T> apply(List<T> source, String query) {
    if (query.isEmpty) return source;
    if (identical(source, _resultSource) && query == _resultQuery) {
      return _result;
    }

    if (!identical(source, _indexedSource)) {
      _lowerFields = [
        for (final item in source)
          [for (final f in _fields(item)) f.toLowerCase()],
      ];
      _indexedSource = source;
    }

    final q = query.toLowerCase();
    final result = <T>[];
    for (var i = 0; i < source.length; i++) {
      if (_lowerFields[i].any((f) => f.contains(q))) result.add(source[i]);
    }

    _resultSource = source;
    _resultQuery = query;
    _result = result;
    return result;
  }
}
