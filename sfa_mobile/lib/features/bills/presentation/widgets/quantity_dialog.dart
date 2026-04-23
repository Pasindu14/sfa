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

    if (hasError) return;

    final finalQty = _unitType == _UnitType.cases
        ? qty! * _packsPerCase
        : qty!;

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
    return Container(
      decoration: BoxDecoration(
        color: AppColors.background,
        borderRadius: BorderRadius.vertical(top: Radius.circular(20.r)),
      ),
      padding: EdgeInsets.only(bottom: bottom),
      child: SingleChildScrollView(
        child: Padding(
          padding: EdgeInsets.fromLTRB(20.w, 12.h, 20.w, 24.h),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // ── Handle ─────────────────────────────────────────────────
              Center(
                child: Container(
                  width: 36.w,
                  height: 4.h,
                  decoration: BoxDecoration(
                    color: AppColors.surfaceVariant,
                    borderRadius: BorderRadius.circular(2.r),
                  ),
                ),
              ),
              SizedBox(height: 16.h),

              // ── Product name + code ─────────────────────────────────────
              Text(
                widget.product.itemDescription,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 20.sp,
                  fontWeight: FontWeight.w700,
                  color: AppColors.foreground,
                ),
              ),
              SizedBox(height: 6.h),
              Container(
                padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 3.h),
                decoration: BoxDecoration(
                  color: AppColors.surfaceVariant,
                  borderRadius: BorderRadius.circular(4.r),
                ),
                child: Text(
                  widget.product.code,
                  style: GoogleFonts.barlow(
                    fontSize: 11.sp,
                    color: AppColors.foregroundMuted,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
              SizedBox(height: 20.h),

              // ── Item type: Sale / Return ─────────────────────────────────
              _sectionLabel('ITEM TYPE'),
              SizedBox(height: 8.h),
              Row(
                children: [
                  Expanded(
                    child: _UnitButton(
                      label: 'Sale',
                      icon: Icons.sell_rounded,
                      selected: !_isReturn,
                      selectedColor: AppColors.primary,
                      onTap: () => _setItemType('Sale'),
                    ),
                  ),
                  SizedBox(width: 10.w),
                  Expanded(
                    child: _UnitButton(
                      label: 'Return',
                      icon: Icons.undo_rounded,
                      selected: _isReturn,
                      selectedColor: AppColors.error,
                      onTap: () => _setItemType('Return'),
                    ),
                  ),
                ],
              ),
              SizedBox(height: 16.h),

              // ── Return type chips (only when Return) ─────────────────────
              if (_isReturn) ...[
                _sectionLabel('RETURN TYPE'),
                if (_returnTypeError != null) ...[
                  SizedBox(height: 4.h),
                  Text(
                    _returnTypeError!,
                    style: GoogleFonts.barlow(
                        fontSize: 11.sp, color: AppColors.error),
                  ),
                ],
                SizedBox(height: 8.h),
                Row(
                  children: [
                    _ReturnTypeChip(
                      label: 'Damage',
                      selected: _returnType == 'Damage',
                      onTap: () => _setReturnType('Damage'),
                    ),
                    SizedBox(width: 10.w),
                    _ReturnTypeChip(
                      label: 'Expire',
                      selected: _returnType == 'Expire',
                      onTap: () => _setReturnType('Expire'),
                    ),
                  ],
                ),
                SizedBox(height: 12.h),

                // ── Expire date picker ──────────────────────────────────────
                if (_returnType == 'Expire') ...[
                  GestureDetector(
                    onTap: _pickExpireDate,
                    child: Container(
                      width: double.infinity,
                      padding: EdgeInsets.symmetric(
                          horizontal: 16.w, vertical: 14.h),
                      decoration: BoxDecoration(
                        color: _expireDate != null
                            ? AppColors.error.withValues(alpha: 0.06)
                            : AppColors.surface,
                        borderRadius: BorderRadius.circular(8.r),
                        border: Border.all(
                          color: _expireDateError != null
                              ? AppColors.error
                              : _expireDate != null
                                  ? AppColors.error.withValues(alpha: 0.4)
                                  : AppColors.surfaceVariant,
                          width: _expireDateError != null ? 1.5 : 1,
                        ),
                      ),
                      child: Row(
                        children: [
                          Icon(
                            Icons.calendar_today_rounded,
                            size: 14.r,
                            color: _expireDate != null
                                ? AppColors.error
                                : AppColors.foregroundMuted,
                          ),
                          SizedBox(width: 10.w),
                          Expanded(
                            child: Text(
                              _expireDate != null
                                  ? _formatDate(_expireDate!)
                                  : 'Select expire date',
                              style: GoogleFonts.barlow(
                                fontSize: 13.sp,
                                color: _expireDate != null
                                    ? AppColors.error
                                    : AppColors.foregroundMuted,
                                fontWeight: _expireDate != null
                                    ? FontWeight.w600
                                    : FontWeight.w400,
                              ),
                            ),
                          ),
                          Icon(Icons.chevron_right_rounded,
                              size: 16.r, color: AppColors.foregroundMuted),
                        ],
                      ),
                    ),
                  ),
                  if (_expireDateError != null) ...[
                    SizedBox(height: 4.h),
                    Text(
                      _expireDateError!,
                      style: GoogleFonts.barlow(
                          fontSize: 11.sp, color: AppColors.error),
                    ),
                  ],
                  SizedBox(height: 16.h),
                ],
              ],

              // ── Unit type (Cases / Packets) ──────────────────────────────
              if (_hasCasesOption) ...[
                _sectionLabel('UNIT TYPE'),
                SizedBox(height: 8.h),
                Row(
                  children: [
                    Expanded(
                      child: _UnitButton(
                        label: 'Cases',
                        icon: Icons.inventory_2_rounded,
                        selected: _unitType == _UnitType.cases,
                        selectedColor: AppColors.primary,
                        onTap: () =>
                            setState(() => _unitType = _UnitType.cases),
                      ),
                    ),
                    SizedBox(width: 10.w),
                    Expanded(
                      child: _UnitButton(
                        label: 'Packets',
                        icon: Icons.local_mall_rounded,
                        selected: _unitType == _UnitType.packets,
                        selectedColor: AppColors.primary,
                        onTap: () =>
                            setState(() => _unitType = _UnitType.packets),
                      ),
                    ),
                  ],
                ),
                SizedBox(height: 16.h),
              ],

              // ── Price display (static for Sale, editable for Return) ──────
              if (_isReturn) ...[
                TextField(
                  controller: _priceController,
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                  inputFormatters: [
                    FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
                  ],
                  decoration: InputDecoration(
                    labelText: 'Return Price (per pack)',
                    prefixText: 'Rs. ',
                    suffixText: '/ pack',
                  ),
                  onChanged: (_) => setState(() {}),
                ),
              ] else ...[
                Row(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    RichText(
                      text: TextSpan(
                        children: [
                          TextSpan(
                            text: 'Rs. ${_packPrice.toStringAsFixed(2)}',
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 20.sp,
                              fontWeight: FontWeight.w800,
                              color: AppColors.primary,
                            ),
                          ),
                          TextSpan(
                            text: ' / pack',
                            style: GoogleFonts.barlow(
                              fontSize: 13.sp,
                              color: AppColors.foregroundMuted,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ],
              if (_unitType == _UnitType.cases && _hasCasesOption) ...[
                SizedBox(height: 2.h),
                Text(
                  '1 case = $_packsPerCase packs',
                  style: GoogleFonts.barlow(
                    fontSize: 12.sp,
                    color: AppColors.foregroundMuted,
                  ),
                ),
              ],
              SizedBox(height: 20.h),

              // ── Qty + Discount ───────────────────────────────────────────
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    flex: _isReturn ? 1 : 3,
                    child: TextField(
                      controller: _qtyController,
                      autofocus: !_isReturn,
                      keyboardType:
                          const TextInputType.numberWithOptions(decimal: true),
                      inputFormatters: [
                        FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
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
                            const TextInputType.numberWithOptions(decimal: true),
                        inputFormatters: [
                          FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
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
                SizedBox(height: 6.h),
                Text(
                  '= ${(_enteredQty * _packsPerCase).toStringAsFixed(0)} packs total',
                  style: GoogleFonts.barlow(
                    fontSize: 12.sp,
                    color: AppColors.foregroundMuted,
                  ),
                ),
              ],
              SizedBox(height: 16.h),

              // ── Line total ───────────────────────────────────────────────
              Container(
                width: double.infinity,
                padding:
                    EdgeInsets.symmetric(horizontal: 16.w, vertical: 14.h),
                decoration: BoxDecoration(
                  color: _isReturn
                      ? AppColors.error.withValues(alpha: 0.06)
                      : AppColors.surface,
                  borderRadius: BorderRadius.circular(8.r),
                  border: Border.all(
                    color: _isReturn
                        ? AppColors.error.withValues(alpha: 0.25)
                        : AppColors.surfaceVariant,
                  ),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      'Line Total',
                      style: GoogleFonts.barlow(
                        fontSize: 13.sp,
                        color: AppColors.foregroundMuted,
                      ),
                    ),
                    Text(
                      _isReturn
                          ? '−Rs. ${_lineTotal.toStringAsFixed(2)}'
                          : 'Rs. ${_lineTotal.toStringAsFixed(2)}',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 20.sp,
                        fontWeight: FontWeight.w800,
                        color: _isReturn
                            ? AppColors.error
                            : (_enteredDisc > 0
                                ? AppColors.success
                                : AppColors.foreground),
                      ),
                    ),
                  ],
                ),
              ),
              SizedBox(height: 20.h),

              // ── Cancel / Add to Cart ─────────────────────────────────────
              Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () => Navigator.of(context).pop(),
                      child: const Text('Cancel'),
                    ),
                  ),
                  SizedBox(width: 12.w),
                  Expanded(
                    flex: 2,
                    child: FilledButton(
                      onPressed: _submit,
                      style: _isReturn
                          ? FilledButton.styleFrom(
                              backgroundColor: AppColors.error)
                          : null,
                      child: Text(_isReturn ? 'Add Return' : 'Add to Cart'),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _sectionLabel(String text) => Text(
        text,
        style: GoogleFonts.barlow(
          fontSize: 11.sp,
          color: AppColors.foregroundMuted,
          fontWeight: FontWeight.w600,
          letterSpacing: 0.8,
        ),
      );

  String _formatDate(DateTime d) =>
      '${d.day.toString().padLeft(2, '0')}/'
      '${d.month.toString().padLeft(2, '0')}/'
      '${d.year}';
}

// ── Sale/Return unit button ────────────────────────────────────────────────────

class _UnitButton extends StatelessWidget {
  final String label;
  final IconData icon;
  final bool selected;
  final Color selectedColor;
  final VoidCallback onTap;

  const _UnitButton({
    required this.label,
    required this.icon,
    required this.selected,
    required this.selectedColor,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: selected ? null : onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: EdgeInsets.symmetric(vertical: 11.h),
        decoration: BoxDecoration(
          color: selected ? selectedColor : AppColors.surface,
          borderRadius: BorderRadius.circular(8.r),
          border: Border.all(
            color: selected ? selectedColor : AppColors.surfaceVariant,
            width: 1.5,
          ),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              icon,
              size: 15.r,
              color: selected ? AppColors.onPrimary : AppColors.foregroundMuted,
            ),
            SizedBox(width: 6.w),
            Text(
              label,
              style: GoogleFonts.barlow(
                fontSize: 13.sp,
                fontWeight: FontWeight.w600,
                color: selected ? AppColors.onPrimary : AppColors.foreground,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Return type chip ──────────────────────────────────────────────────────────

class _ReturnTypeChip extends StatelessWidget {
  final String label;
  final bool selected;
  final VoidCallback onTap;

  const _ReturnTypeChip({
    required this.label,
    required this.selected,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: GestureDetector(
        onTap: selected ? null : onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 150),
          padding: EdgeInsets.symmetric(vertical: 11.h),
          decoration: BoxDecoration(
            color: selected
                ? AppColors.error.withValues(alpha: 0.10)
                : AppColors.surface,
            borderRadius: BorderRadius.circular(8.r),
            border: Border.all(
              color: selected
                  ? AppColors.error
                  : AppColors.surfaceVariant,
              width: selected ? 1.5 : 1,
            ),
          ),
          child: Center(
            child: Text(
              label,
              style: GoogleFonts.barlow(
                fontSize: 13.sp,
                fontWeight: FontWeight.w600,
                color: selected ? AppColors.error : AppColors.foreground,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
