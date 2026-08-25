import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';

class QuantityDialogResult {
  final double quantity;
  final double unitPrice;
  final double discountRate;
  final String billingItemType;
  final String? returnType;
  final String? freeIssueSource;
  final DateTime? expireDate;
  final String priceType;

  const QuantityDialogResult({
    required this.quantity,
    required this.unitPrice,
    this.discountRate = 0,
    this.billingItemType = 'Sale',
    this.returnType,
    this.freeIssueSource,
    this.expireDate,
    this.priceType = 'Packet',
  });
}

enum _Mode { sale, freeIssue, returnItem }

/// One tab's in-progress numbers. Kept per entry key so switching tabs (or
/// return types) never carries a quantity across to a different line.
typedef _QtyEntry = ({
  String cases,
  String packets,
  String disc,
  String price,
  DateTime? expireDate,
});

String _modeToBillingItemType(_Mode m) {
  switch (m) {
    case _Mode.sale:       return 'Sale';
    case _Mode.freeIssue:  return 'FreeIssue';
    case _Mode.returnItem: return 'Return';
  }
}

Future<List<QuantityDialogResult>?> showQuantityDialog(
  BuildContext context, {
  required ProductWithPrice product,
}) {
  return showModalBottomSheet<List<QuantityDialogResult>>(
    context: context,
    isScrollControlled: true,
    backgroundColor: Colors.transparent,
    builder: (ctx) => _QuantitySheet(product: product),
  );
}

class _QuantitySheet extends StatefulWidget {
  final ProductWithPrice product;
  const _QuantitySheet({required this.product});

  @override
  State<_QuantitySheet> createState() => _QuantitySheetState();
}

class _QuantitySheetState extends State<_QuantitySheet> {
  _Mode _mode = _Mode.sale;
  String? _returnType;
  String _freeIssueSource = 'Company';
  DateTime? _expireDate;

  late final TextEditingController _casesController;
  late final TextEditingController _packetsController;
  final TextEditingController _discController = TextEditingController(text: '0');
  late final TextEditingController _priceController;

  String? _qtyError;
  String? _discError;
  String? _returnTypeError;
  String? _expireDateError;
  String? _priceError;

  // Sale / Free Issue / Return — and each return type (Damage, Expire, Resell)
  // — are independent entries. Each remembers its own cases/packets/discount/
  // price/expire date instead of sharing the same text fields.
  final Map<String, _QtyEntry> _entryByKey = {};

  @override
  void initState() {
    super.initState();
    // SFA-117: Cases and Packets start blank — the rep fills both in.
    _casesController = TextEditingController();
    _packetsController = TextEditingController();
    _priceController = TextEditingController(text: _defaultPriceText);
  }

  @override
  void dispose() {
    _casesController.dispose();
    _packetsController.dispose();
    _discController.dispose();
    _priceController.dispose();
    super.dispose();
  }

  bool get _hasCasesOption => widget.product.packsPerCase > 1;
  bool get _isReturn    => _mode == _Mode.returnItem;
  bool get _isFreeIssue => _mode == _Mode.freeIssue;
  Color get _accentColor {
    switch (_mode) {
      case _Mode.returnItem: return AppColors.error;
      case _Mode.freeIssue:  return AppColors.success;
      case _Mode.sale:       return AppColors.primary;
    }
  }

  double get _packPrice => widget.product.dealerPackPrice ?? 0.0;
  double get _casePrice => widget.product.dealerCasePrice ?? (_packPrice * _packsPerCase);
  int get _packsPerCase => widget.product.packsPerCase;

  /// Identifies the entry the text fields currently hold. Return lines are
  /// keyed per return type, so Damage, Expire and Resell each keep their own
  /// quantity and price.
  String get _entryKey => _mode == _Mode.returnItem
      ? 'Return:${_returnType ?? ''}'
      : _modeToBillingItemType(_mode);

  String get _defaultPriceText =>
      (widget.product.dealerPackPrice ?? 0).toStringAsFixed(0);

  void _stashEntry(String key) {
    _entryByKey[key] = (
      cases: _casesController.text,
      packets: _packetsController.text,
      disc: _discController.text,
      price: _priceController.text,
      expireDate: _expireDate,
    );
  }

  void _restoreEntry(String key) {
    final saved = _entryByKey[key];
    _casesController.text = saved?.cases ?? '';
    _packetsController.text = saved?.packets ?? '';
    _discController.text = saved?.disc ?? '0';
    _priceController.text = saved?.price ?? _defaultPriceText;
    _expireDate = saved?.expireDate;
  }

  void _setMode(_Mode mode) {
    if (mode == _mode) return;
    final previousKey = _entryKey;
    setState(() {
      _stashEntry(previousKey);
      _mode = mode;
      switch (mode) {
        case _Mode.sale:
        case _Mode.freeIssue:
          _returnType = null;
          _returnTypeError = null;
          _expireDateError = null;
          break;
        case _Mode.returnItem:
          _returnType = 'Damage';
          _returnTypeError = null;
          break;
      }
      _restoreEntry(_entryKey);
      if (_mode != _Mode.returnItem) _expireDate = null;
      _qtyError = null;
      _discError = null;
    });
  }

  void _setReturnType(String type) {
    if (type == _returnType) {
      setState(() => _returnTypeError = null);
      return;
    }
    final previousKey = _entryKey;
    setState(() {
      _stashEntry(previousKey);
      _returnType = type;
      _returnTypeError = null;
      _expireDateError = null;
      _restoreEntry(_entryKey);
      if (type != 'Expire') _expireDate = null;
      _qtyError = null;
      _discError = null;
    });
  }

  Future<void> _pickExpireDate() async {
    final today = DateTime.now();
    final picked = await showDatePicker(
      context: context,
      initialDate: _expireDate ?? today,
      firstDate: DateTime(today.year - 5),
      lastDate: today,
      helpText: 'Select expire date',
    );
    if (picked != null) {
      setState(() {
        _expireDate = picked;
        _expireDateError = null;
      });
    }
  }

  // ── Staged entries ─────────────────────────────────────────────────────────
  //
  // The rep can fill in Sale, Free Issue and any of the three return types for
  // one product and add them all with a single press. Each becomes its own cart
  // line; _entryByKey holds the ones not currently on screen.

  /// Every key the sheet can hold, in the order the lines are handed to the
  /// cart. Sale first so the bill reads the way a rep would write it.
  static const List<String> _allEntryKeys = [
    'Sale',
    'FreeIssue',
    'Return:Damage',
    'Return:Expire',
    'Return:MarketResell',
  ];

  String _billingTypeForKey(String key) =>
      key.startsWith('Return:') ? 'Return' : key;

  String? _returnTypeForKey(String key) =>
      key.startsWith('Return:') ? key.substring('Return:'.length) : null;

  /// Snapshot of the text fields as they stand right now.
  _QtyEntry _currentEntry() => (
        cases: _casesController.text,
        packets: _packetsController.text,
        disc: _discController.text,
        price: _priceController.text,
        expireDate: _expireDate,
      );

  double _casesOf(_QtyEntry e) => double.tryParse(e.cases.trim()) ?? 0;
  double _packetsOf(_QtyEntry e) => double.tryParse(e.packets.trim()) ?? 0;
  double _discOf(_QtyEntry e) => double.tryParse(e.disc.trim()) ?? 0;
  double _priceOf(_QtyEntry e) => double.tryParse(e.price.trim()) ?? 0;

  /// Everything the rep has actually put a number against, in display order.
  ///
  /// Overlays the live fields on top of the stored map rather than calling
  /// _stashEntry, because this is read from build() and must not mutate state.
  List<({String key, _QtyEntry entry})> get _stagedEntries {
    final live = {..._entryByKey, _entryKey: _currentEntry()};
    return [
      for (final key in _allEntryKeys)
        if (live[key] != null &&
            (_casesOf(live[key]!) > 0 || _packetsOf(live[key]!) > 0))
          (key: key, entry: live[key]!),
    ];
  }

  /// Money impact of one staged entry, matching how CreateBillState aggregates
  /// the cart: sales charge net of the line discount, free issues carry a value
  /// but cost nothing, returns are a credit back to the distributor.
  double _entryTotal(String key, _QtyEntry e) {
    final type = _billingTypeForKey(key);
    if (type == 'Return') {
      final packs = (_casesOf(e) * _packsPerCase) + _packetsOf(e);
      return packs * _priceOf(e);
    }
    final gross = (_casesOf(e) * _casePrice) + (_packetsOf(e) * _packPrice);
    if (type == 'FreeIssue') return gross;
    return gross * (1 - _discOf(e) / 100);
  }

  /// Net effect on the bill of everything staged — free issues excluded because
  /// they are not charged, returns subtracted because they are a credit.
  double get _stagedNet {
    var net = 0.0;
    for (final staged in _stagedEntries) {
      final type = _billingTypeForKey(staged.key);
      if (type == 'FreeIssue') continue;
      final value = _entryTotal(staged.key, staged.entry);
      net += type == 'Return' ? -value : value;
    }
    return net;
  }

  /// First problem with a staged entry, or null when it is ready to add.
  /// [field] names the inline error slot to light up once the sheet has been
  /// switched to that entry.
  ({String field, String message})? _validateEntry(String key, _QtyEntry e) {
    if (_casesOf(e) < 0 || _packetsOf(e) < 0) {
      return (field: 'qty', message: 'Quantity cannot be negative.');
    }
    if (key == 'Sale') {
      final disc = double.tryParse(e.disc.trim());
      if (disc == null || disc < 0 || disc > 100) {
        return (field: 'disc', message: 'Enter a discount between 0 and 100.');
      }
    }
    if (_billingTypeForKey(key) == 'Return') {
      if (_priceOf(e) <= 0) {
        return (
          field: 'price',
          message: 'Enter a return price greater than zero.'
        );
      }
      if (_returnTypeForKey(key) == 'Expire' && e.expireDate == null) {
        return (field: 'expire', message: 'Select an expire date.');
      }
    }
    return null;
  }

  /// Brings the offending entry on screen before reporting its error, so the
  /// rep never gets a complaint about a tab they cannot see.
  void _focusEntry(String key, ({String field, String message}) problem) {
    final targetMode = key == 'Sale'
        ? _Mode.sale
        : key == 'FreeIssue'
            ? _Mode.freeIssue
            : _Mode.returnItem;
    if (targetMode != _mode) _setMode(targetMode);

    final returnType = _returnTypeForKey(key);
    if (returnType != null && returnType != _returnType) {
      _setReturnType(returnType);
    }

    setState(() {
      _qtyError = problem.field == 'qty' ? problem.message : null;
      _discError = problem.field == 'disc' ? problem.message : null;
      _expireDateError = problem.field == 'expire' ? problem.message : null;
      _priceError = problem.field == 'price' ? problem.message : null;
    });
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text(message,
          style: GoogleFonts.barlow(
              color: Colors.white, fontWeight: FontWeight.w500)),
      backgroundColor: AppColors.error,
      behavior: SnackBarBehavior.floating,
      margin: EdgeInsets.all(16.w),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8.r)),
      duration: const Duration(seconds: 3),
    ));
  }

  /// Cart lines for one staged entry. Cases and packets stay separate lines so
  /// each keeps its own priceType — that is what the cart merges on.
  List<QuantityDialogResult> _resultsFor(String key, _QtyEntry e) {
    final cases = _casesOf(e);
    final packets = _packetsOf(e);
    final type = _billingTypeForKey(key);
    final isReturn = type == 'Return';
    final isFree = type == 'FreeIssue';
    final disc = (isReturn || isFree) ? 0.0 : _discOf(e);
    final source = isFree ? _freeIssueSource : null;
    final returnType = _returnTypeForKey(key);

    return [
      if (cases > 0 && _hasCasesOption)
        QuantityDialogResult(
          quantity: cases * _packsPerCase,
          unitPrice: isReturn ? _priceOf(e) : _casePrice / _packsPerCase,
          discountRate: disc,
          billingItemType: type,
          returnType: returnType,
          freeIssueSource: source,
          expireDate: e.expireDate,
          priceType: 'Case',
        ),
      if (packets > 0)
        QuantityDialogResult(
          quantity: packets,
          unitPrice: isReturn ? _priceOf(e) : _packPrice,
          discountRate: disc,
          billingItemType: type,
          returnType: returnType,
          freeIssueSource: source,
          expireDate: e.expireDate,
          priceType: 'Packet',
        ),
    ];
  }

  void _submit() {
    final staged = _stagedEntries;

    if (staged.isEmpty) {
      const message = 'Enter a quantity for at least one item type.';
      setState(() => _qtyError = message);
      _showError(message);
      return;
    }

    for (final entry in staged) {
      final problem = _validateEntry(entry.key, entry.entry);
      if (problem == null) continue;
      _focusEntry(entry.key, problem);
      _showError(problem.message);
      return;
    }

    setState(() {
      _qtyError = null;
      _discError = null;
      _returnTypeError = null;
      _expireDateError = null;
      _priceError = null;
    });

    Navigator.of(context).pop([
      for (final entry in staged) ..._resultsFor(entry.key, entry.entry),
    ]);
  }

  // ── Staged summary presentation ────────────────────────────────────────────

  /// Which segments of the ITEM TYPE track hold a quantity. Drives the dot that
  /// tells the rep a tab they are not looking at is still staged.
  Set<int> get _filledSegments {
    final keys = _stagedEntries.map((e) => e.key).toSet();
    return {
      if (keys.contains('Sale')) 0,
      if (keys.contains('FreeIssue')) 1,
      if (keys.any((k) => k.startsWith('Return:'))) 2,
    };
  }

  bool _returnTypeFilled(String type) =>
      _stagedEntries.any((e) => e.key == 'Return:$type');

  String get _addButtonLabel {
    final count = _stagedEntries.length;
    if (count == 0) return 'Add to Cart';
    if (count == 1) return 'Add 1 line';
    return 'Add $count lines';
  }

  String _returnLabel(String type) => type == 'MarketResell' ? 'Resell' : type;

  ({String label, Color color}) _summaryStyleFor(String key) {
    switch (_billingTypeForKey(key)) {
      case 'FreeIssue':
        return (label: 'Free Issue', color: AppColors.success);
      case 'Return':
        return (
          label: 'Return · ${_returnLabel(_returnTypeForKey(key)!)}',
          color: AppColors.error
        );
      default:
        return (label: 'Sale', color: AppColors.primary);
    }
  }

  /// '2 cs + 4 pkt' — only the units the rep actually entered.
  String _qtyLabel(_QtyEntry e) {
    final cases = _casesOf(e);
    final packets = _packetsOf(e);
    return [
      if (cases > 0 && _hasCasesOption) '${_trimQty(cases)} cs',
      if (packets > 0) '${_trimQty(packets)} pkt',
    ].join(' + ');
  }

  String _trimQty(double v) =>
      v == v.roundToDouble() ? v.toStringAsFixed(0) : v.toStringAsFixed(2);

  Widget _summaryHeading(String text) => Text(
        text,
        style: GoogleFonts.barlowCondensed(
          fontSize: 10.sp,
          fontWeight: FontWeight.w600,
          letterSpacing: 1.5,
          color: Colors.white.withValues(alpha: 0.40),
        ),
      );

  Widget _summaryRow(String key, _QtyEntry entry) {
    final style = _summaryStyleFor(key);
    final type = _billingTypeForKey(key);
    final value = _entryTotal(key, entry);
    final amount = type == 'FreeIssue'
        ? 'FOC'
        : type == 'Return'
            ? '−Rs. ${value.toStringAsFixed(2)}'
            : 'Rs. ${value.toStringAsFixed(2)}';

    return Row(
      children: [
        Container(
          width: 6.r,
          height: 6.r,
          decoration: BoxDecoration(color: style.color, shape: BoxShape.circle),
        ),
        SizedBox(width: 8.w),
        Expanded(
          child: Text(
            style.label,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: GoogleFonts.barlow(
              fontSize: 12.sp,
              fontWeight: FontWeight.w600,
              color: Colors.white.withValues(alpha: 0.75),
            ),
          ),
        ),
        SizedBox(width: 8.w),
        Text(
          _qtyLabel(entry),
          style: GoogleFonts.barlowCondensed(
            fontSize: 13.sp,
            fontWeight: FontWeight.w700,
            letterSpacing: 0.3,
            color: Colors.white.withValues(alpha: 0.55),
          ),
        ),
        SizedBox(width: 12.w),
        Text(
          amount,
          style: GoogleFonts.barlowCondensed(
            fontSize: 15.sp,
            fontWeight: FontWeight.w800,
            letterSpacing: -0.2,
            color: style.color,
          ),
        ),
      ],
    );
  }

  /// Card listing every staged line and the net effect on the bill. Replaces
  /// the old single LINE TOTAL, which could only describe the visible tab.
  Widget _buildStagedSummary() {
    final staged = _stagedEntries;
    final net = _stagedNet;

    return Container(
      width: double.infinity,
      padding: EdgeInsets.symmetric(horizontal: 18.w, vertical: 16.h),
      decoration: BoxDecoration(
        color: AppColors.darkSurface,
        borderRadius: BorderRadius.circular(14.r),
      ),
      child: staged.isEmpty
          ? Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _summaryHeading('NOTHING STAGED YET'),
                SizedBox(height: 6.h),
                Text(
                  'Enter a quantity to begin. Sale, Free Issue and Returns can '
                  'all go on this item in one go.',
                  style: GoogleFonts.barlow(
                    fontSize: 11.sp,
                    height: 1.35,
                    color: Colors.white.withValues(alpha: 0.30),
                  ),
                ),
              ],
            )
          : Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                _summaryHeading(staged.length == 1
                    ? '1 LINE STAGED'
                    : '${staged.length} LINES STAGED'),
                SizedBox(height: 12.h),
                for (var i = 0; i < staged.length; i++) ...[
                  if (i > 0) SizedBox(height: 9.h),
                  _summaryRow(staged[i].key, staged[i].entry),
                ],
                SizedBox(height: 12.h),
                Container(
                  height: 1,
                  color: Colors.white.withValues(alpha: 0.10),
                ),
                SizedBox(height: 12.h),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  crossAxisAlignment: CrossAxisAlignment.baseline,
                  textBaseline: TextBaseline.alphabetic,
                  children: [
                    _summaryHeading('NET'),
                    Text(
                      net < 0
                          ? '−Rs. ${net.abs().toStringAsFixed(2)}'
                          : 'Rs. ${net.toStringAsFixed(2)}',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 24.sp,
                        fontWeight: FontWeight.w900,
                        letterSpacing: -0.5,
                        color: net < 0 ? AppColors.error : AppColors.amber,
                      ),
                    ),
                  ],
                ),
              ],
            ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final bottom = MediaQuery.of(context).viewInsets.bottom;

    return ScrollConfiguration(
      behavior: ScrollConfiguration.of(context).copyWith(overscroll: false),
      child: AnimatedContainer(
      duration: const Duration(milliseconds: 200),
      decoration: BoxDecoration(
        color: AppColors.background,
        borderRadius: BorderRadius.vertical(top: Radius.circular(24.r)),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Padding(
            padding: EdgeInsets.only(top: 10.h),
            child: Column(
              children: [
                Container(
                  width: 36.w,
                  height: 4.h,
                  decoration: BoxDecoration(
                    color: AppColors.surfaceVariant,
                    borderRadius: BorderRadius.circular(2.r),
                  ),
                ),
                SizedBox(height: 8.h),
                AnimatedContainer(
                  duration: const Duration(milliseconds: 250),
                  curve: Curves.easeOut,
                  width: 40.w,
                  height: 3.h,
                  decoration: BoxDecoration(
                    color: _accentColor,
                    borderRadius: BorderRadius.circular(2.r),
                  ),
                ),
              ],
            ),
          ),

          Flexible(
            child: SingleChildScrollView(
              physics: const ClampingScrollPhysics(),
              keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
              padding: EdgeInsets.fromLTRB(20.w, 16.h, 20.w,
                  bottom > 0 ? bottom + 16.h : 28.h),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    widget.product.itemDescription,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 22.sp,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 0.1,
                      color: AppColors.foreground,
                    ),
                  ),
                  SizedBox(height: 6.h),
                  Row(
                    children: [
                      Container(
                        padding: EdgeInsets.symmetric(
                            horizontal: 8.w, vertical: 3.h),
                        decoration: BoxDecoration(
                          color: _accentColor.withValues(alpha: 0.10),
                          borderRadius: BorderRadius.circular(4.r),
                          border: Border.all(
                              color: _accentColor.withValues(alpha: 0.25)),
                        ),
                        child: Text(
                          widget.product.code,
                          style: GoogleFonts.barlow(
                            fontSize: 11.sp,
                            color: _accentColor,
                            fontWeight: FontWeight.w700,
                            letterSpacing: 0.3,
                          ),
                        ),
                      ),
                    ],
                  ),

                  if (widget.product.normalStock != null) ...[
                    SizedBox(height: 8.h),
                    _StockInfoRow(qty: widget.product.normalStock!),
                  ],

                  SizedBox(height: 20.h),
                  _Divider(),
                  SizedBox(height: 16.h),

                  _sectionLabel('ITEM TYPE'),
                  SizedBox(height: 8.h),
                  _SegmentedTrack(
                    segments: const [
                      _Segment('Sale',       Icons.sell_rounded),
                      _Segment('Free Issue', Icons.card_giftcard_rounded),
                      _Segment('Return',     Icons.undo_rounded),
                    ],
                    selectedIndex: _mode == _Mode.sale
                        ? 0
                        : _mode == _Mode.freeIssue
                            ? 1
                            : 2,
                    filledIndices: _filledSegments,
                    activeColor: _accentColor,
                    onChanged: (i) => _setMode(
                      i == 0
                          ? _Mode.sale
                          : i == 1
                              ? _Mode.freeIssue
                              : _Mode.returnItem,
                    ),
                  ),

                  AnimatedSize(
                    duration: const Duration(milliseconds: 220),
                    curve: Curves.easeInOut,
                    child: _isFreeIssue
                        ? Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              SizedBox(height: 16.h),
                              _Divider(),
                              SizedBox(height: 14.h),
                              _sectionLabel('FUNDED BY'),
                              SizedBox(height: 10.h),
                              Row(
                                children: [
                                  _SourceChip(
                                    label: 'Company',
                                    icon: Icons.business_rounded,
                                    selected: _freeIssueSource == 'Company',
                                    onTap: () => setState(
                                        () => _freeIssueSource = 'Company'),
                                  ),
                                  SizedBox(width: 10.w),
                                  _SourceChip(
                                    label: 'Distributor',
                                    icon: Icons.local_shipping_rounded,
                                    selected: _freeIssueSource == 'Distributor',
                                    onTap: () => setState(
                                        () => _freeIssueSource = 'Distributor'),
                                  ),
                                ],
                              ),
                            ],
                          )
                        : const SizedBox.shrink(),
                  ),

                  AnimatedSize(
                    duration: const Duration(milliseconds: 220),
                    curve: Curves.easeInOut,
                    child: _isReturn
                        ? Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              SizedBox(height: 16.h),
                              _Divider(),
                              SizedBox(height: 14.h),
                              Row(
                                children: [
                                  _sectionLabel('RETURN TYPE'),
                                  if (_returnTypeError != null) ...[
                                    SizedBox(width: 8.w),
                                    Text(
                                      '· ${_returnTypeError!}',
                                      style: GoogleFonts.barlow(
                                        fontSize: 11.sp,
                                        color: AppColors.error,
                                      ),
                                    ),
                                  ],
                                ],
                              ),
                              SizedBox(height: 10.h),
                              Row(
                                children: [
                                  _ReturnChip(
                                    label: 'Damage',
                                    icon: Icons.warning_amber_rounded,
                                    selected: _returnType == 'Damage',
                                    filled: _returnTypeFilled('Damage'),
                                    onTap: () => _setReturnType('Damage'),
                                  ),
                                  SizedBox(width: 10.w),
                                  _ReturnChip(
                                    label: 'Expire',
                                    icon: Icons.event_rounded,
                                    selected: _returnType == 'Expire',
                                    filled: _returnTypeFilled('Expire'),
                                    onTap: () => _setReturnType('Expire'),
                                  ),
                                  SizedBox(width: 10.w),
                                  _ReturnChip(
                                    label: 'Resell',
                                    icon: Icons.storefront_rounded,
                                    selected: _returnType == 'MarketResell',
                                    filled: _returnTypeFilled('MarketResell'),
                                    onTap: () => _setReturnType('MarketResell'),
                                  ),
                                ],
                              ),

                              AnimatedSize(
                                duration: const Duration(milliseconds: 200),
                                curve: Curves.easeInOut,
                                child: _returnType == 'Expire'
                                    ? Column(
                                        crossAxisAlignment:
                                            CrossAxisAlignment.start,
                                        children: [
                                          SizedBox(height: 12.h),
                                          GestureDetector(
                                            onTap: _pickExpireDate,
                                            child: AnimatedContainer(
                                              duration: const Duration(
                                                  milliseconds: 200),
                                              padding: EdgeInsets.symmetric(
                                                  horizontal: 14.w,
                                                  vertical: 13.h),
                                              decoration: BoxDecoration(
                                                color: _expireDate != null
                                                    ? AppColors.error
                                                        .withValues(alpha: 0.06)
                                                    : AppColors.surface,
                                                borderRadius:
                                                    BorderRadius.circular(10.r),
                                                border: Border.all(
                                                  color: _expireDateError != null
                                                      ? AppColors.error
                                                      : _expireDate != null
                                                          ? AppColors.error
                                                              .withValues(
                                                                  alpha: 0.45)
                                                          : AppColors
                                                              .surfaceVariant,
                                                  width: 1.5,
                                                ),
                                              ),
                                              child: Row(
                                                children: [
                                                  Container(
                                                    width: 28.r,
                                                    height: 28.r,
                                                    decoration: BoxDecoration(
                                                      color: _expireDate != null
                                                          ? AppColors.error
                                                              .withValues(
                                                                  alpha: 0.12)
                                                          : AppColors
                                                              .surfaceVariant,
                                                      borderRadius:
                                                          BorderRadius.circular(
                                                              7.r),
                                                    ),
                                                    child: Icon(
                                                      Icons
                                                          .calendar_today_rounded,
                                                      size: 14.r,
                                                      color: _expireDate != null
                                                          ? AppColors.error
                                                          : AppColors
                                                              .foregroundMuted,
                                                    ),
                                                  ),
                                                  SizedBox(width: 12.w),
                                                  Expanded(
                                                    child: Text(
                                                      _expireDate != null
                                                          ? _formatDate(
                                                              _expireDate!)
                                                          : 'Select expire date',
                                                      style: GoogleFonts.barlow(
                                                        fontSize: 13.sp,
                                                        color: _expireDate !=
                                                                null
                                                            ? AppColors.error
                                                            : AppColors
                                                                .foregroundMuted,
                                                        fontWeight:
                                                            _expireDate != null
                                                                ? FontWeight.w600
                                                                : FontWeight.w400,
                                                      ),
                                                    ),
                                                  ),
                                                  Icon(
                                                    Icons
                                                        .chevron_right_rounded,
                                                    size: 18.r,
                                                    color: _expireDate != null
                                                        ? AppColors.error
                                                            .withValues(
                                                                alpha: 0.5)
                                                        : AppColors
                                                            .foregroundMuted,
                                                  ),
                                                ],
                                              ),
                                            ),
                                          ),
                                          if (_expireDateError != null)
                                            Padding(
                                              padding: EdgeInsets.only(
                                                  top: 4.h, left: 4.w),
                                              child: Text(
                                                _expireDateError!,
                                                style: GoogleFonts.barlow(
                                                    fontSize: 11.sp,
                                                    color: AppColors.error),
                                              ),
                                            ),
                                        ],
                                      )
                                    : const SizedBox.shrink(),
                              ),
                            ],
                          )
                        : const SizedBox.shrink(),
                  ),

                  SizedBox(height: 16.h),
                  _Divider(),
                  SizedBox(height: 16.h),

                  if (_isReturn) ...[
                    _sectionLabel('RETURN PRICE'),
                    SizedBox(height: 8.h),
                    TextField(
                      controller: _priceController,
                      keyboardType:
                          const TextInputType.numberWithOptions(decimal: true),
                      inputFormatters: [
                        FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
                      ],
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 20.sp,
                        fontWeight: FontWeight.w700,
                        color: AppColors.foreground,
                      ),
                      decoration: InputDecoration(
                        prefixText: 'Rs. ',
                        prefixStyle: GoogleFonts.barlow(
                          fontSize: 14.sp,
                          color: AppColors.foregroundMuted,
                        ),
                        suffixText: '/ pack',
                        suffixStyle: GoogleFonts.barlow(
                          fontSize: 12.sp,
                          color: AppColors.foregroundMuted,
                        ),
                        errorText: _priceError,
                      ),
                      onChanged: (_) => setState(() {}),
                    ),
                  ] else ...[
                    if (_hasCasesOption) ...[
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.baseline,
                        textBaseline: TextBaseline.alphabetic,
                        children: [
                          Text('Rs.', style: GoogleFonts.barlow(fontSize: 13.sp, color: AppColors.foregroundMuted)),
                          SizedBox(width: 4.w),
                          Text(_casePrice.toStringAsFixed(2), style: GoogleFonts.barlowCondensed(fontSize: 22.sp, fontWeight: FontWeight.w800, letterSpacing: -0.5, color: AppColors.primary)),
                          SizedBox(width: 4.w),
                          Text('/ case', style: GoogleFonts.barlow(fontSize: 11.sp, color: AppColors.foregroundMuted)),
                          SizedBox(width: 16.w),
                          Text('Rs.', style: GoogleFonts.barlow(fontSize: 13.sp, color: AppColors.foregroundMuted)),
                          SizedBox(width: 4.w),
                          Text(_packPrice.toStringAsFixed(2), style: GoogleFonts.barlowCondensed(fontSize: 22.sp, fontWeight: FontWeight.w800, letterSpacing: -0.5, color: AppColors.foreground.withValues(alpha: 0.60))),
                          SizedBox(width: 4.w),
                          Text('/ pack', style: GoogleFonts.barlow(fontSize: 11.sp, color: AppColors.foregroundMuted)),
                        ],
                      ),
                    ] else ...[
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.baseline,
                        textBaseline: TextBaseline.alphabetic,
                        children: [
                          Text('Rs.', style: GoogleFonts.barlow(fontSize: 14.sp, color: AppColors.foregroundMuted)),
                          SizedBox(width: 4.w),
                          Text(_packPrice.toStringAsFixed(2), style: GoogleFonts.barlowCondensed(fontSize: 26.sp, fontWeight: FontWeight.w800, letterSpacing: -0.5, color: AppColors.primary)),
                          SizedBox(width: 4.w),
                          Text('/ pack', style: GoogleFonts.barlow(fontSize: 12.sp, color: AppColors.foregroundMuted)),
                        ],
                      ),
                    ],
                  ],

                  SizedBox(height: 18.h),

                  if (_hasCasesOption && !_isReturn) ...[
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          child: TextField(
                            controller: _casesController,
                            keyboardType: const TextInputType.numberWithOptions(decimal: true),
                            inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))],
                            decoration: const InputDecoration(labelText: 'Cases', hintText: '0'),
                            onChanged: (_) => setState(() {}),
                            onSubmitted: (_) => _submit(),
                          ),
                        ),
                        SizedBox(width: 10.w),
                        Expanded(
                          child: TextField(
                            controller: _packetsController,
                            autofocus: true,
                            keyboardType: const TextInputType.numberWithOptions(decimal: true),
                            inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))],
                            decoration: InputDecoration(labelText: 'Packets', hintText: '0', errorText: _qtyError),
                            onChanged: (_) => setState(() {}),
                            onSubmitted: (_) => _submit(),
                          ),
                        ),
                        if (_mode == _Mode.sale) ...[
                          SizedBox(width: 10.w),
                          Expanded(
                            child: TextField(
                              controller: _discController,
                              keyboardType: const TextInputType.numberWithOptions(decimal: true),
                              inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))],
                              decoration: InputDecoration(labelText: 'Discount', hintText: '0', suffixText: '%', errorText: _discError),
                              onChanged: (_) => setState(() {}),
                              onSubmitted: (_) => _submit(),
                            ),
                          ),
                        ],
                      ],
                    ),
                  ] else ...[
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          flex: _isReturn ? 1 : 3,
                          child: TextField(
                            controller: _packetsController,
                            autofocus: !_isReturn,
                            keyboardType: const TextInputType.numberWithOptions(decimal: true),
                            inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))],
                            decoration: InputDecoration(
                              labelText: 'Quantity',
                              hintText: '# packs',
                              errorText: _qtyError,
                            ),
                            onChanged: (_) => setState(() {}),
                            onSubmitted: (_) => _submit(),
                          ),
                        ),
                        if (_mode == _Mode.sale) ...[
                          SizedBox(width: 12.w),
                          Expanded(
                            flex: 2,
                            child: TextField(
                              controller: _discController,
                              keyboardType: const TextInputType.numberWithOptions(decimal: true),
                              inputFormatters: [FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))],
                              decoration: InputDecoration(labelText: 'Discount', hintText: '0', suffixText: '%', errorText: _discError),
                              onChanged: (_) => setState(() {}),
                              onSubmitted: (_) => _submit(),
                            ),
                          ),
                        ],
                      ],
                    ),
                  ],

                  SizedBox(height: 18.h),

                  _buildStagedSummary(),

                  SizedBox(height: 16.h),

                  Row(
                    children: [
                      SizedBox(
                        height: 50.h,
                        child: OutlinedButton(
                          onPressed: () => Navigator.of(context).pop(),
                          style: OutlinedButton.styleFrom(
                            padding: EdgeInsets.symmetric(horizontal: 20.w),
                            side: BorderSide(color: AppColors.surfaceVariant, width: 1.5),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12.r),
                            ),
                            foregroundColor: AppColors.foregroundMuted,
                          ),
                          child: Text(
                            'Cancel',
                            style: GoogleFonts.barlow(
                              fontSize: 14.sp,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                        ),
                      ),
                      SizedBox(width: 12.w),
                      Expanded(
                        child: SizedBox(
                          height: 50.h,
                          child: FilledButton(
                            onPressed: _submit,
                            style: FilledButton.styleFrom(
                              backgroundColor: _accentColor,
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12.r),
                              ),
                              elevation: 0,
                            ),
                            child: Row(
                              mainAxisAlignment: MainAxisAlignment.center,
                              children: [
                                Icon(
                                  _isReturn
                                      ? Icons.undo_rounded
                                      : _isFreeIssue
                                          ? Icons.card_giftcard_rounded
                                          : Icons.add_shopping_cart_rounded,
                                  size: 16.r,
                                  color: Colors.white,
                                ),
                                SizedBox(width: 8.w),
                                Text(
                                  _addButtonLabel,
                                  style: GoogleFonts.barlowCondensed(
                                    fontSize: 16.sp,
                                    fontWeight: FontWeight.w700,
                                    letterSpacing: 0.5,
                                    color: Colors.white,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
      ),
    );
  }

  Widget _sectionLabel(String text) => Text(
        text,
        style: GoogleFonts.barlowCondensed(
          fontSize: 10.sp,
          fontWeight: FontWeight.w700,
          letterSpacing: 1.8,
          color: AppColors.foregroundMuted,
        ),
      );

  String _formatDate(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')} / '
      '${d.month.toString().padLeft(2, '0')} / '
      '${d.year}';
}

// ── Segmented track control ───────────────────────────────────────────────────

class _Segment {
  final String label;
  final IconData icon;
  const _Segment(this.label, this.icon);
}

class _SegmentedTrack extends StatelessWidget {
  final List<_Segment> segments;
  final int selectedIndex;
  final Color activeColor;
  final ValueChanged<int> onChanged;

  /// Segments holding a quantity. Marked with a dot so the rep can see that a
  /// tab they are not looking at is still staged.
  final Set<int> filledIndices;

  const _SegmentedTrack({
    required this.segments,
    required this.selectedIndex,
    required this.activeColor,
    required this.onChanged,
    this.filledIndices = const {},
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 46.h,
      padding: EdgeInsets.all(3.r),
      decoration: BoxDecoration(
        color: AppColors.surfaceVariant,
        borderRadius: BorderRadius.circular(11.r),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: segments.asMap().entries.map((entry) {
          final i = entry.key;
          final seg = entry.value;
          final isActive = selectedIndex == i;
          return Expanded(
            child: GestureDetector(
              onTap: isActive ? null : () => onChanged(i),
              child: AnimatedContainer(
                duration: const Duration(milliseconds: 200),
                curve: Curves.easeInOut,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  color: isActive
                      ? activeColor.withValues(alpha: 0.12)
                      : Colors.transparent,
                  borderRadius: BorderRadius.circular(8.r),
                  border: isActive
                      ? Border.all(
                          color: activeColor.withValues(alpha: 0.35), width: 1)
                      : null,
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(
                      seg.icon,
                      size: 14.r,
                      color: isActive
                          ? activeColor
                          : AppColors.foregroundMuted,
                    ),
                    SizedBox(width: 5.w),
                    Flexible(
                      child: Text(
                        seg.label,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: GoogleFonts.barlow(
                          fontSize: 13.sp,
                          fontWeight:
                              isActive ? FontWeight.w700 : FontWeight.w500,
                          color: isActive
                              ? activeColor
                              : AppColors.foregroundMuted,
                        ),
                      ),
                    ),
                    if (filledIndices.contains(i)) ...[
                      SizedBox(width: 4.w),
                      Container(
                        width: 5.r,
                        height: 5.r,
                        decoration: BoxDecoration(
                          color: isActive
                              ? activeColor
                              : AppColors.foreground.withValues(alpha: 0.55),
                          shape: BoxShape.circle,
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ),
          );
        }).toList(),
      ),
    );
  }
}

// ── Return type chip ──────────────────────────────────────────────────────────

class _ReturnChip extends StatelessWidget {
  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onTap;

  /// This return type already holds a quantity.
  final bool filled;

  const _ReturnChip({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onTap,
    this.filled = false,
  });

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: GestureDetector(
        onTap: selected ? null : onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 180),
          curve: Curves.easeOut,
          padding: EdgeInsets.symmetric(vertical: 12.h),
          decoration: BoxDecoration(
            color: selected
                ? AppColors.error.withValues(alpha: 0.10)
                : AppColors.surface,
            borderRadius: BorderRadius.circular(10.r),
            border: Border.all(
              color: selected
                  ? AppColors.error.withValues(alpha: 0.55)
                  : AppColors.surfaceVariant,
              width: 1.5,
            ),
          ),
          child: Column(
            children: [
              Icon(
                icon,
                size: 18.r,
                color: selected ? AppColors.error : AppColors.foregroundMuted,
              ),
              SizedBox(height: 4.h),
              Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    label,
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 0.3,
                      color: selected ? AppColors.error : AppColors.foreground,
                    ),
                  ),
                  if (filled) ...[
                    SizedBox(width: 4.w),
                    Container(
                      width: 5.r,
                      height: 5.r,
                      decoration: BoxDecoration(
                        color: selected
                            ? AppColors.error
                            : AppColors.foreground.withValues(alpha: 0.55),
                        shape: BoxShape.circle,
                      ),
                    ),
                  ],
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Free issue funding source chip ────────────────────────────────────────────

class _SourceChip extends StatelessWidget {
  final String label;
  final IconData icon;
  final bool selected;
  final VoidCallback onTap;

  const _SourceChip({
    required this.label,
    required this.icon,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: GestureDetector(
        onTap: selected ? null : onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 180),
          curve: Curves.easeOut,
          padding: EdgeInsets.symmetric(vertical: 12.h),
          decoration: BoxDecoration(
            color: selected
                ? AppColors.success.withValues(alpha: 0.10)
                : AppColors.surface,
            borderRadius: BorderRadius.circular(10.r),
            border: Border.all(
              color: selected
                  ? AppColors.success.withValues(alpha: 0.55)
                  : AppColors.surfaceVariant,
              width: 1.5,
            ),
          ),
          child: Column(
            children: [
              Icon(
                icon,
                size: 18.r,
                color:
                    selected ? AppColors.success : AppColors.foregroundMuted,
              ),
              SizedBox(height: 4.h),
              Text(
                label,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 13.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 0.3,
                  color:
                      selected ? AppColors.success : AppColors.foreground,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Thin section divider ──────────────────────────────────────────────────────

class _Divider extends StatelessWidget {
  @override
  Widget build(BuildContext context) => Divider(
        height: 1,
        thickness: 1,
        color: AppColors.surfaceVariant,
      );
}

// ── Distributor stock info row ────────────────────────────────────────────────

class _StockInfoRow extends StatelessWidget {
  const _StockInfoRow({required this.qty});
  final double qty;

  @override
  Widget build(BuildContext context) {
    final hasStock = qty > 0;
    final color = hasStock ? AppColors.success : AppColors.warning;
    final icon = hasStock ? Icons.check_circle_outline_rounded : Icons.warning_amber_rounded;

    return Container(
      padding: EdgeInsets.symmetric(horizontal: 10.w, vertical: 6.h),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.07),
        borderRadius: BorderRadius.circular(8.r),
        border: Border.all(color: color.withValues(alpha: 0.20)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(icon, size: 13.r, color: color),
          SizedBox(width: 6.w),
          Text(
            hasStock
                ? 'Available stock: ${qty.toStringAsFixed(0)} units'
                : 'No stock available',
            style: GoogleFonts.barlow(
              fontSize: 11.sp,
              fontWeight: FontWeight.w600,
              color: color,
            ),
          ),
        ],
      ),
    );
  }
}
