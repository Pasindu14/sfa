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
  final DateTime? expireDate;

  const QuantityDialogResult({
    required this.quantity,
    required this.unitPrice,
    this.discountRate = 0,
    this.billingItemType = 'Sale',
    this.returnType,
    this.expireDate,
  });
}

enum _UnitType { cases, packets }

Future<QuantityDialogResult?> showQuantityDialog(
  BuildContext context, {
  required ProductWithPrice product,
}) {
  return showModalBottomSheet<QuantityDialogResult>(
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
  _UnitType _unitType = _UnitType.packets;
  String _billingItemType = 'Sale';
  String? _returnType;
  DateTime? _expireDate;

  final TextEditingController _qtyController =
      TextEditingController(text: '1');
  final TextEditingController _discController =
      TextEditingController(text: '0');
  late final TextEditingController _priceController;

  String? _qtyError;
  String? _discError;
  String? _returnTypeError;
  String? _expireDateError;

  @override
  void initState() {
    super.initState();
    _priceController = TextEditingController(
      text: (widget.product.dealerPackPrice ?? 0).toStringAsFixed(0),
    );
  }

  @override
  void dispose() {
    _qtyController.dispose();
    _discController.dispose();
    _priceController.dispose();
    super.dispose();
  }

  bool get _hasCasesOption => widget.product.packsPerCase > 1;
  bool get _isReturn => _billingItemType == 'Return';
  Color get _accentColor => _isReturn ? AppColors.error : AppColors.primary;

  double get _packPrice => widget.product.dealerPackPrice ?? 0.0;
  double get _returnPrice =>
      double.tryParse(_priceController.text.trim()) ?? 0;
  int get _packsPerCase => widget.product.packsPerCase;

  double get _enteredQty =>
      double.tryParse(_qtyController.text.trim()) ?? 0;
  double get _enteredDisc =>
      double.tryParse(_discController.text.trim()) ?? 0;

  double get _qtyInPacks =>
      _unitType == _UnitType.cases ? _enteredQty * _packsPerCase : _enteredQty;

  double get _lineTotal {
    if (_isReturn) return _qtyInPacks * _returnPrice;
    final gross = _qtyInPacks * _packPrice;
    return gross * (1 - _enteredDisc / 100);
  }

  void _setItemType(String type) {
    setState(() {
      _billingItemType = type;
      if (type == 'Sale') {
        _returnType = null;
        _expireDate = null;
        _returnTypeError = null;
        _expireDateError = null;
      } else {
        _returnType = 'Damage';
        _returnTypeError = null;
      }
    });
  }

  void _setReturnType(String type) {
    setState(() {
      _returnType = type;
      _returnTypeError = null;
      if (type != 'Expire') _expireDate = null;
      _expireDateError = null;
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

  void _submit() {
    bool hasError = false;

    final qty = double.tryParse(_qtyController.text.trim());
    if (qty == null || qty <= 0) {
      setState(() => _qtyError = 'Enter a quantity greater than zero.');
      hasError = true;
    } else {
      setState(() => _qtyError = null);
    }

    if (!_isReturn) {
      final disc = double.tryParse(_discController.text.trim());
      if (disc == null || disc < 0 || disc > 100) {
        setState(() => _discError = 'Enter a value between 0 and 100.');
        hasError = true;
      } else {
        setState(() => _discError = null);
      }
    }

    if (_isReturn) {
      if (_returnType == null) {
        setState(() => _returnTypeError = 'Select a return type.');
        hasError = true;
      }
      if (_returnType == 'Expire' && _expireDate == null) {
        setState(() => _expireDateError = 'Select an expire date.');
        hasError = true;
      }
    }

    if (hasError) {
      final msg = _qtyError ?? _returnTypeError ?? _expireDateError ?? _discError;
      if (msg != null) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(msg, style: GoogleFonts.barlow(fontWeight: FontWeight.w600)),
            backgroundColor: AppColors.error,
            behavior: SnackBarBehavior.floating,
            duration: const Duration(seconds: 3),
          ),
        );
      }
      return;
    }

    final finalQty =
        _unitType == _UnitType.cases ? qty! * _packsPerCase : qty!;
    final disc = double.tryParse(_discController.text.trim()) ?? 0;

    Navigator.of(context).pop(QuantityDialogResult(
      quantity: finalQty,
      unitPrice: _isReturn ? _returnPrice : _packPrice,
      discountRate: _isReturn ? 0 : disc,
      billingItemType: _billingItemType,
      returnType: _returnType,
      expireDate: _expireDate,
    ));
  }

  @override
  Widget build(BuildContext context) {
    final bottom = MediaQuery.of(context).viewInsets.bottom;

    return AnimatedContainer(
      duration: const Duration(milliseconds: 200),
      decoration: BoxDecoration(
        color: AppColors.background,
        borderRadius: BorderRadius.vertical(top: Radius.circular(24.r)),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          // ── Drag handle + mode accent bar ────────────────────────────────
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

          // ── Scrollable body ──────────────────────────────────────────────
          Flexible(
            child: SingleChildScrollView(
              keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
              padding: EdgeInsets.fromLTRB(20.w, 16.h, 20.w,
                  bottom > 0 ? bottom + 16.h : 28.h),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ── Product header ────────────────────────────────────────
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

                  SizedBox(height: 20.h),
                  _Divider(),
                  SizedBox(height: 16.h),

                  // ── Item type toggle ──────────────────────────────────────
                  _sectionLabel('ITEM TYPE'),
                  SizedBox(height: 8.h),
                  _SegmentedTrack(
                    segments: const [
                      _Segment('Sale', Icons.sell_rounded),
                      _Segment('Return', Icons.undo_rounded),
                    ],
                    selectedIndex: _isReturn ? 1 : 0,
                    activeColor: _accentColor,
                    onChanged: (i) => _setItemType(i == 0 ? 'Sale' : 'Return'),
                  ),

                  // ── Return type (animated) ────────────────────────────────
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
                                    onTap: () => _setReturnType('Damage'),
                                  ),
                                  SizedBox(width: 10.w),
                                  _ReturnChip(
                                    label: 'Expire',
                                    icon: Icons.event_rounded,
                                    selected: _returnType == 'Expire',
                                    onTap: () => _setReturnType('Expire'),
                                  ),
                                  SizedBox(width: 10.w),
                                  _ReturnChip(
                                    label: 'Resell',
                                    icon: Icons.storefront_rounded,
                                    selected: _returnType == 'MarketResell',
                                    onTap: () => _setReturnType('MarketResell'),
                                  ),
                                ],
                              ),

                              // ── Expire date ───────────────────────────────
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

                  // ── Unit type (Cases / Packets) ───────────────────────────
                  if (_hasCasesOption) ...[
                    _sectionLabel('UNIT TYPE'),
                    SizedBox(height: 8.h),
                    _SegmentedTrack(
                      segments: const [
                        _Segment('Cases', Icons.inventory_2_rounded),
                        _Segment('Packets', Icons.local_mall_rounded),
                      ],
                      selectedIndex:
                          _unitType == _UnitType.cases ? 0 : 1,
                      activeColor: AppColors.primary,
                      onChanged: (i) => setState(() =>
                          _unitType = i == 0
                              ? _UnitType.cases
                              : _UnitType.packets),
                    ),
                    SizedBox(height: 16.h),
                  ],

                  // ── Price ─────────────────────────────────────────────────
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
                      ),
                      onChanged: (_) => setState(() {}),
                    ),
                  ] else ...[
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.baseline,
                      textBaseline: TextBaseline.alphabetic,
                      children: [
                        Text(
                          'Rs.',
                          style: GoogleFonts.barlow(
                            fontSize: 14.sp,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                        SizedBox(width: 4.w),
                        Text(
                          _packPrice.toStringAsFixed(2),
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 26.sp,
                            fontWeight: FontWeight.w800,
                            letterSpacing: -0.5,
                            color: AppColors.primary,
                          ),
                        ),
                        SizedBox(width: 4.w),
                        Text(
                          '/ pack',
                          style: GoogleFonts.barlow(
                            fontSize: 12.sp,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                      ],
                    ),
                  ],
                  if (_unitType == _UnitType.cases && _hasCasesOption) ...[
                    SizedBox(height: 4.h),
                    Text(
                      '1 case = $_packsPerCase packs',
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        color: AppColors.foregroundMuted,
                      ),
                    ),
                  ],

                  SizedBox(height: 18.h),

                  // ── Quantity + Discount ───────────────────────────────────
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        flex: _isReturn ? 1 : 3,
                        child: TextField(
                          controller: _qtyController,
                          autofocus: !_isReturn,
                          keyboardType: const TextInputType.numberWithOptions(
                              decimal: true),
                          inputFormatters: [
                            FilteringTextInputFormatter.allow(
                                RegExp(r'[0-9.]')),
                          ],
                          decoration: InputDecoration(
                            labelText: 'Quantity',
                            hintText: _unitType == _UnitType.cases
                                ? '# cases'
                                : '# packs',
                            errorText: _qtyError,
                          ),
                          onChanged: (_) => setState(() {}),
                          onSubmitted: (_) => _submit(),
                        ),
                      ),
                      if (!_isReturn) ...[
                        SizedBox(width: 12.w),
                        Expanded(
                          flex: 2,
                          child: TextField(
                            controller: _discController,
                            keyboardType:
                                const TextInputType.numberWithOptions(
                                    decimal: true),
                            inputFormatters: [
                              FilteringTextInputFormatter.allow(
                                  RegExp(r'[0-9.]')),
                            ],
                            decoration: InputDecoration(
                              labelText: 'Discount',
                              hintText: '0',
                              suffixText: '%',
                              errorText: _discError,
                            ),
                            onChanged: (_) => setState(() {}),
                            onSubmitted: (_) => _submit(),
                          ),
                        ),
                      ],
                    ],
                  ),
                  if (_unitType == _UnitType.cases &&
                      _enteredQty > 0 &&
                      _hasCasesOption) ...[
                    SizedBox(height: 5.h),
                    Text(
                      '= ${(_enteredQty * _packsPerCase).toStringAsFixed(0)} packs total',
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        color: AppColors.foregroundMuted,
                      ),
                    ),
                  ],

                  SizedBox(height: 18.h),

                  // ── Line total — dark premium card ────────────────────────
                  Container(
                    width: double.infinity,
                    padding: EdgeInsets.symmetric(
                        horizontal: 18.w, vertical: 16.h),
                    decoration: BoxDecoration(
                      color: AppColors.darkSurface,
                      borderRadius: BorderRadius.circular(14.r),
                    ),
                    child: Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'LINE TOTAL',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 10.sp,
                                fontWeight: FontWeight.w600,
                                letterSpacing: 1.5,
                                color: Colors.white.withValues(alpha: 0.40),
                              ),
                            ),
                            SizedBox(height: 2.h),
                            Text(
                              _isReturn ? 'Credit to distributor' : 'Charged to outlet',
                              style: GoogleFonts.barlow(
                                fontSize: 11.sp,
                                color: Colors.white.withValues(alpha: 0.30),
                              ),
                            ),
                          ],
                        ),
                        Text(
                          _isReturn
                              ? '−Rs. ${_lineTotal.toStringAsFixed(2)}'
                              : 'Rs. ${_lineTotal.toStringAsFixed(2)}',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 24.sp,
                            fontWeight: FontWeight.w900,
                            letterSpacing: -0.5,
                            color: _isReturn
                                ? AppColors.error
                                : (_enteredDisc > 0
                                    ? AppColors.success
                                    : AppColors.amber),
                          ),
                        ),
                      ],
                    ),
                  ),

                  SizedBox(height: 16.h),

                  // ── Action buttons ────────────────────────────────────────
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
                                      : Icons.add_shopping_cart_rounded,
                                  size: 16.r,
                                  color: Colors.white,
                                ),
                                SizedBox(width: 8.w),
                                Text(
                                  _isReturn ? 'Add Return' : 'Add to Cart',
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

  const _SegmentedTrack({
    required this.segments,
    required this.selectedIndex,
    required this.activeColor,
    required this.onChanged,
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
                    Text(
                      seg.label,
                      style: GoogleFonts.barlow(
                        fontSize: 13.sp,
                        fontWeight:
                            isActive ? FontWeight.w700 : FontWeight.w500,
                        color: isActive
                            ? activeColor
                            : AppColors.foregroundMuted,
                      ),
                    ),
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

  const _ReturnChip({
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
              Text(
                label,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 13.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 0.3,
                  color: selected ? AppColors.error : AppColors.foreground,
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
