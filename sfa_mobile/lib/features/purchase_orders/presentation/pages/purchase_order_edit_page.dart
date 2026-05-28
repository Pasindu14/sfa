import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/purchase_orders/domain/entities/editable_order_item.dart';
import 'package:uswatte/features/purchase_orders/domain/entities/product_with_price.dart';
import 'package:uswatte/features/purchase_orders/domain/entities/purchase_order_detail.dart';
import 'package:uswatte/features/purchase_orders/domain/usecases/get_products_for_distributor_usecase.dart';
import 'package:uswatte/features/purchase_orders/domain/usecases/update_purchase_order_usecase.dart';

// ── Page ──────────────────────────────────────────────────────────────────────

class PurchaseOrderEditPage extends StatefulWidget {
  final PurchaseOrderDetail order;
  final UpdatePurchaseOrderUseCase updateOrder;
  final GetProductsForDistributorUseCase getProducts;

  const PurchaseOrderEditPage({
    super.key,
    required this.order,
    required this.updateOrder,
    required this.getProducts,
  });

  @override
  State<PurchaseOrderEditPage> createState() => _PurchaseOrderEditPageState();
}

class _PurchaseOrderEditPageState extends State<PurchaseOrderEditPage> {
  late final TextEditingController _notesController;
  late List<EditableOrderItem> _items;
  List<ProductWithPrice> _availableProducts = [];
  bool _isLoadingProducts = true;
  bool _isSaving = false;
  String? _loadError;

  @override
  void initState() {
    super.initState();
    _notesController = TextEditingController(text: widget.order.notes ?? '');
    _items = widget.order.items
        .map((i) => EditableOrderItem(
              productId: i.productId,
              productCode: i.productCode,
              productDescription: i.productDescription,
              unitPrice: i.unitPrice,
              quantity: i.quantity,
              discount: i.discount,
            ))
        .toList();
    _loadProducts();
  }

  @override
  void dispose() {
    _notesController.dispose();
    super.dispose();
  }

  Future<void> _loadProducts() async {
    setState(() {
      _isLoadingProducts = true;
      _loadError = null;
    });
    try {
      final products = await widget.getProducts(widget.order.distributorId);
      if (mounted) {
        setState(() {
          _availableProducts = products;
          _isLoadingProducts = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          _loadError = 'Could not load product pricing.';
          _isLoadingProducts = false;
        });
      }
    }
  }

  double get _total => _items.fold(0.0, (s, i) => s + i.lineTotal);

  void _increment(int index) => setState(() => _items[index].quantity++);

  void _decrement(int index) {
    if (_items[index].quantity > 1) setState(() => _items[index].quantity--);
  }

  void _remove(int index) => setState(() => _items.removeAt(index));

  void _addProduct(ProductWithPrice p) {
    final idx = _items.indexWhere((i) => i.productId == p.productId);
    if (idx >= 0) {
      setState(() => _items[idx].quantity++);
    } else {
      setState(() => _items.add(EditableOrderItem(
            productId: p.productId,
            productCode: p.productCode,
            productDescription: p.itemDescription,
            unitPrice: p.unitPrice,
            quantity: 1,
          )));
    }
  }

  Future<void> _save() async {
    if (_items.isEmpty) {
      _showSnackbar('Add at least one item before saving.', isError: true);
      return;
    }
    setState(() => _isSaving = true);
    try {
      final notes = _notesController.text.trim();
      await widget.updateOrder(
        widget.order.id,
        _items,
        notes.isEmpty ? null : notes,
      );
      if (mounted) {
        _showSnackbar('Order updated successfully.', isError: false);
        context.pop(true);
      }
    } catch (e) {
      if (mounted) {
        setState(() => _isSaving = false);
        _showSnackbar(
          e.toString().replaceFirst('Exception: ', ''),
          isError: true,
        );
      }
    }
  }

  void _showSnackbar(String msg, {required bool isError}) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Row(children: [
        Icon(
          isError
              ? Icons.error_outline_rounded
              : Icons.check_circle_outline_rounded,
          color: Colors.white,
          size: 18.r,
        ),
        SizedBox(width: 10.w),
        Expanded(
          child: Text(msg,
              style: GoogleFonts.barlow(
                  fontSize: 13.sp, fontWeight: FontWeight.w500)),
        ),
      ]),
      backgroundColor:
          isError ? AppColors.error : AppColors.success,
      behavior: SnackBarBehavior.floating,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10.r)),
      margin: EdgeInsets.all(16.r),
    ));
  }

  void _openProductPicker() {
    if (_availableProducts.isEmpty) return;
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _ProductPickerSheet(
        products: _availableProducts,
        onSelected: (p) {
          _addProduct(p);
          Navigator.pop(context);
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return Scaffold(
      backgroundColor: const Color(0xFFF8F7F5),
      body: _isLoadingProducts
          ? _buildLoadingState()
          : _loadError != null
              ? _buildErrorState()
              : _buildContent(),
    );
  }

  Widget _buildLoadingState() {
    return CustomScrollView(
      slivers: [
        _buildSliverHeader(),
        const SliverFillRemaining(
          child: Center(child: AppSpinner()),
        ),
      ],
    );
  }

  Widget _buildErrorState() {
    return CustomScrollView(
      slivers: [
        _buildSliverHeader(),
        SliverFillRemaining(
          child: Center(
            child: Padding(
              padding: EdgeInsets.all(32.r),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Container(
                    width: 64.r,
                    height: 64.r,
                    decoration: BoxDecoration(
                      color: AppColors.error.withValues(alpha: 0.08),
                      shape: BoxShape.circle,
                    ),
                    child: Icon(Icons.wifi_off_rounded,
                        color: AppColors.error, size: 30.r),
                  ),
                  SizedBox(height: 16.h),
                  Text(
                    _loadError!,
                    textAlign: TextAlign.center,
                    style: GoogleFonts.barlow(
                        fontSize: 14.sp,
                        color: AppColors.foregroundMuted),
                  ),
                  SizedBox(height: 16.h),
                  OutlinedButton.icon(
                    onPressed: _loadProducts,
                    icon: Icon(Icons.refresh_rounded,
                        size: 16.r, color: AppColors.primary),
                    label: Text('Retry',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 14.sp,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 0.8,
                          color: AppColors.primary,
                        )),
                    style: OutlinedButton.styleFrom(
                      side: BorderSide(
                          color: AppColors.primary.withValues(alpha: 0.4)),
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(10.r)),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildContent() {
    return Stack(
      children: [
        CustomScrollView(
          slivers: [
            _buildSliverHeader(),
            SliverToBoxAdapter(child: _buildItemsSection()),
            SliverToBoxAdapter(child: _buildNotesSection()),
            SliverToBoxAdapter(child: SizedBox(height: 100.h)),
          ],
        ),
        Positioned(
          left: 0,
          right: 0,
          bottom: 0,
          child: _buildBottomBar(),
        ),
      ],
    );
  }

  // ── Sliver header ────────────────────────────────────────────────────────────

  SliverAppBar _buildSliverHeader() {
    return SliverAppBar(
      expandedHeight: 170.h,
      pinned: true,
      backgroundColor: AppColors.primaryDark,
      leading: GestureDetector(
        onTap: () => context.canPop() ? context.pop() : null,
        child: Container(
          margin: EdgeInsets.all(8.r),
          decoration: BoxDecoration(
            color: Colors.white.withValues(alpha: 0.15),
            borderRadius: BorderRadius.circular(10.r),
            border: Border.all(color: Colors.white.withValues(alpha: 0.25)),
          ),
          child: Icon(Icons.arrow_back_ios_new_rounded,
              size: 15.r, color: Colors.white),
        ),
      ),
      title: Text(
        'EDIT ORDER',
        style: GoogleFonts.barlowCondensed(
          fontSize: 16.sp,
          fontWeight: FontWeight.w800,
          letterSpacing: 1.2,
          color: Colors.white,
        ),
      ),
      flexibleSpace: FlexibleSpaceBar(
        collapseMode: CollapseMode.pin,
        background: Container(
          decoration: const BoxDecoration(
            gradient: LinearGradient(
              begin: Alignment.topLeft,
              end: Alignment.bottomRight,
              colors: [AppColors.primaryDark, AppColors.primary],
            ),
          ),
          child: SafeArea(
            child: Padding(
              padding: EdgeInsets.fromLTRB(20.w, 48.h, 20.w, 20.h),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      // EDIT MODE badge
                      Container(
                        padding: EdgeInsets.symmetric(
                            horizontal: 10.w, vertical: 4.h),
                        decoration: BoxDecoration(
                          color: Colors.white.withValues(alpha: 0.15),
                          borderRadius: BorderRadius.circular(4.r),
                          border: Border.all(
                              color: Colors.white.withValues(alpha: 0.3),
                              width: 0.8),
                        ),
                        child: Text(
                          'EDIT MODE',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 9.sp,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 2.0,
                            color: Colors.white,
                          ),
                        ),
                      ),
                      SizedBox(height: 8.h),
                      // Order number
                      Text(
                        widget.order.orderNumber,
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 34.sp,
                          fontWeight: FontWeight.w900,
                          letterSpacing: 1.5,
                          height: 1.0,
                          color: Colors.white,
                        ),
                      ),
                      SizedBox(height: 4.h),
                      // Distributor + category
                      Row(
                        children: [
                          Icon(Icons.storefront_outlined,
                              size: 12.r,
                              color: Colors.white.withValues(alpha: 0.65)),
                          SizedBox(width: 5.w),
                          Text(
                            widget.order.distributorName,
                            style: GoogleFonts.barlow(
                              fontSize: 12.sp,
                              fontWeight: FontWeight.w500,
                              color: Colors.white.withValues(alpha: 0.75),
                            ),
                          ),
                          SizedBox(width: 8.w),
                          Container(
                            padding: EdgeInsets.symmetric(
                                horizontal: 7.w, vertical: 2.h),
                            decoration: BoxDecoration(
                              color: Colors.white.withValues(alpha: 0.18),
                              borderRadius: BorderRadius.circular(4.r),
                            ),
                            child: Text(
                              'CAT ${widget.order.distributorCategory}',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 9.sp,
                                fontWeight: FontWeight.w800,
                                letterSpacing: 1.0,
                                color: Colors.white,
                              ),
                            ),
                          ),
                        ],
                      ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  // ── Items section ─────────────────────────────────────────────────────────────

  Widget _buildItemsSection() {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _SectionHeader(
            icon: Icons.inventory_2_outlined,
            title: 'LINE ITEMS',
            badge: '${_items.length}',
          ),
          SizedBox(height: 10.h),

          // Item cards
          if (_items.isNotEmpty)
            Container(
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(16.r),
                boxShadow: [
                  BoxShadow(
                    color:
                        const Color(0xFF1C1917).withValues(alpha: 0.06),
                    blurRadius: 16,
                    offset: const Offset(0, 4),
                  ),
                ],
              ),
              child: ClipRRect(
                borderRadius: BorderRadius.circular(16.r),
                child: Column(
                  children: List.generate(_items.length, (i) {
                    final isLast = i == _items.length - 1;
                    return _EditableItemRow(
                      item: _items[i],
                      showDivider: !isLast,
                      onIncrement: () => _increment(i),
                      onDecrement: () => _decrement(i),
                      onRemove: () => _remove(i),
                    );
                  }),
                ),
              ),
            ),

          SizedBox(height: 10.h),

          // Add product button
          GestureDetector(
            onTap: _openProductPicker,
            child: Container(
              padding: EdgeInsets.symmetric(vertical: 16.h),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(16.r),
                border: Border.all(
                  color: AppColors.primary.withValues(alpha: 0.35),
                ),
                boxShadow: [
                  BoxShadow(
                    color: AppColors.primary.withValues(alpha: 0.06),
                    blurRadius: 12,
                    offset: const Offset(0, 3),
                  ),
                ],
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Container(
                    width: 28.r,
                    height: 28.r,
                    decoration: BoxDecoration(
                      color: AppColors.primary.withValues(alpha: 0.1),
                      borderRadius: BorderRadius.circular(8.r),
                    ),
                    child: Icon(Icons.add_rounded,
                        size: 16.r, color: AppColors.primary),
                  ),
                  SizedBox(width: 10.w),
                  Text(
                    'ADD PRODUCT',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 1.5,
                      color: AppColors.primary,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }

  // ── Notes section ─────────────────────────────────────────────────────────────

  Widget _buildNotesSection() {
    return Padding(
      padding: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 0),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const _SectionHeader(
            icon: Icons.notes_rounded,
            title: 'NOTES',
          ),
          SizedBox(height: 10.h),
          Container(
            decoration: BoxDecoration(
              color: Colors.white,
              borderRadius: BorderRadius.circular(16.r),
              boxShadow: [
                BoxShadow(
                  color: const Color(0xFF1C1917).withValues(alpha: 0.06),
                  blurRadius: 16,
                  offset: const Offset(0, 4),
                ),
              ],
            ),
            child: TextField(
              controller: _notesController,
              maxLines: 3,
              style: GoogleFonts.barlow(
                  fontSize: 14.sp,
                  color: AppColors.foreground),
              decoration: InputDecoration(
                hintText: 'Optional notes for this order...',
                hintStyle: GoogleFonts.barlow(
                    fontSize: 13.sp,
                    color: AppColors.foregroundMuted),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(16.r),
                  borderSide: BorderSide.none,
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(16.r),
                  borderSide: BorderSide.none,
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(16.r),
                  borderSide: BorderSide(
                      color: AppColors.primary.withValues(alpha: 0.4),
                      width: 1.5),
                ),
                filled: true,
                fillColor: Colors.white,
                contentPadding:
                    EdgeInsets.symmetric(horizontal: 16.w, vertical: 14.h),
              ),
            ),
          ),
        ],
      ),
    );
  }

  // ── Bottom save bar ───────────────────────────────────────────────────────────

  Widget _buildBottomBar() {
    return Container(
      padding: EdgeInsets.fromLTRB(
          16.w, 14.h, 16.w, MediaQuery.of(context).padding.bottom + 14.h),
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF1C1917).withValues(alpha: 0.1),
            blurRadius: 20,
            offset: const Offset(0, -6),
          ),
        ],
      ),
      child: Row(
        children: [
          // Total display
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                'ORDER TOTAL',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 9.sp,
                  fontWeight: FontWeight.w800,
                  letterSpacing: 2.0,
                  color: AppColors.foregroundMuted,
                ),
              ),
              SizedBox(height: 2.h),
              Text(
                'Rs. ${_total.toStringAsFixed(2)}',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 22.sp,
                  fontWeight: FontWeight.w900,
                  letterSpacing: 0.5,
                  height: 1.1,
                  color: AppColors.foreground,
                ),
              ),
              if (_items.isNotEmpty)
                Text(
                  '${_items.length} item${_items.length == 1 ? '' : 's'}',
                  style: GoogleFonts.barlow(
                    fontSize: 10.sp,
                    color: AppColors.foregroundMuted,
                  ),
                ),
            ],
          ),
          const Spacer(),
          // Save button
          IntrinsicWidth(
            child: SizedBox(
            height: 52.h,
            child: ElevatedButton(
              onPressed: _isSaving ? null : _save,
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primaryDark,
                disabledBackgroundColor:
                    AppColors.foregroundMuted.withValues(alpha: 0.4),
                elevation: 0,
                padding:
                    EdgeInsets.symmetric(horizontal: 24.w, vertical: 0),
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12.r)),
              ),
              child: _isSaving
                  ? const AppSpinner.button()
                  : Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.save_rounded,
                            size: 16.r, color: Colors.white),
                        SizedBox(width: 8.w),
                        Text(
                          'SAVE CHANGES',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 14.sp,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 1.2,
                            color: Colors.white,
                          ),
                        ),
                      ],
                    ),
            ),
          ),
          ),  // IntrinsicWidth
        ],
      ),
    );
  }
}

// ── Editable item row ─────────────────────────────────────────────────────────

class _EditableItemRow extends StatelessWidget {
  final EditableOrderItem item;
  final bool showDivider;
  final VoidCallback onIncrement;
  final VoidCallback onDecrement;
  final VoidCallback onRemove;

  const _EditableItemRow({
    required this.item,
    required this.showDivider,
    required this.onIncrement,
    required this.onDecrement,
    required this.onRemove,
  });

  @override
  Widget build(BuildContext context) {
    return Dismissible(
      key: ValueKey(item.productId),
      direction: DismissDirection.endToStart,
      background: Container(
        alignment: Alignment.centerRight,
        padding: EdgeInsets.only(right: 20.w),
        color: AppColors.error.withValues(alpha: 0.08),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.delete_outline_rounded,
                color: AppColors.error, size: 20.r),
            SizedBox(height: 2.h),
            Text(
              'REMOVE',
              style: GoogleFonts.barlowCondensed(
                fontSize: 9.sp,
                fontWeight: FontWeight.w800,
                letterSpacing: 1.0,
                color: AppColors.error,
              ),
            ),
          ],
        ),
      ),
      onDismissed: (_) => onRemove(),
      child: Column(
        children: [
          IntrinsicHeight(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // Left orange accent bar
                Container(
                  width: 4.w,
                  color: AppColors.primary,
                ),
                // Item content
                Expanded(
                  child: Padding(
                    padding: EdgeInsets.fromLTRB(12.w, 14.h, 8.w, 14.h),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.center,
                      children: [
                        // Product info
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              // Code badge
                              Container(
                                padding: EdgeInsets.symmetric(
                                    horizontal: 7.w, vertical: 2.h),
                                decoration: BoxDecoration(
                                  color: AppColors.primary
                                      .withValues(alpha: 0.1),
                                  borderRadius:
                                      BorderRadius.circular(4.r),
                                ),
                                child: Text(
                                  item.productCode,
                                  style: GoogleFonts.barlowCondensed(
                                    fontSize: 9.sp,
                                    fontWeight: FontWeight.w800,
                                    letterSpacing: 0.8,
                                    color: AppColors.primary,
                                  ),
                                ),
                              ),
                              SizedBox(height: 3.h),
                              Text(
                                item.productDescription,
                                style: GoogleFonts.barlow(
                                  fontSize: 13.sp,
                                  fontWeight: FontWeight.w600,
                                  color: AppColors.foreground,
                                ),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis,
                              ),
                              SizedBox(height: 2.h),
                              Text(
                                'Rs. ${item.lineTotal.toStringAsFixed(2)}',
                                style: GoogleFonts.barlowCondensed(
                                  fontSize: 13.sp,
                                  fontWeight: FontWeight.w700,
                                  color: AppColors.foregroundMuted,
                                  letterSpacing: 0.3,
                                ),
                              ),
                            ],
                          ),
                        ),
                        SizedBox(width: 8.w),
                        // Qty stepper
                        _QtyController(
                          qty: item.quantity,
                          onIncrement: onIncrement,
                          onDecrement: onDecrement,
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),  // IntrinsicHeight
          if (showDivider)
            Divider(
              height: 1,
              indent: 16.w,
              endIndent: 16.w,
              color: const Color(0xFF1C1917).withValues(alpha: 0.06),
            ),
        ],
      ),
    );
  }
}

// ── Qty controller ────────────────────────────────────────────────────────────

class _QtyController extends StatelessWidget {
  final int qty;
  final VoidCallback onIncrement;
  final VoidCallback onDecrement;

  const _QtyController({
    required this.qty,
    required this.onIncrement,
    required this.onDecrement,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.surface,
        borderRadius: BorderRadius.circular(10.r),
        border: Border.all(
            color: const Color(0xFF1C1917).withValues(alpha: 0.08)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          _StepBtn(
            icon: Icons.remove_rounded,
            onTap: qty > 1 ? onDecrement : null,
          ),
          SizedBox(
            width: 32.w,
            child: Text(
              '$qty',
              textAlign: TextAlign.center,
              style: GoogleFonts.barlowCondensed(
                fontSize: 16.sp,
                fontWeight: FontWeight.w900,
                color: AppColors.foreground,
              ),
            ),
          ),
          _StepBtn(
            icon: Icons.add_rounded,
            onTap: onIncrement,
          ),
        ],
      ),
    );
  }
}

class _StepBtn extends StatelessWidget {
  final IconData icon;
  final VoidCallback? onTap;
  const _StepBtn({required this.icon, this.onTap});

  @override
  Widget build(BuildContext context) {
    final active = onTap != null;
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 34.r,
        height: 34.r,
        decoration: BoxDecoration(
          color: active
              ? AppColors.primary.withValues(alpha: 0.08)
              : Colors.transparent,
          borderRadius: BorderRadius.circular(9.r),
        ),
        child: Icon(
          icon,
          size: 15.r,
          color: active ? AppColors.primary : AppColors.foregroundMuted.withValues(alpha: 0.4),
        ),
      ),
    );
  }
}

// ── Product picker sheet ──────────────────────────────────────────────────────

class _ProductPickerSheet extends StatefulWidget {
  final List<ProductWithPrice> products;
  final void Function(ProductWithPrice) onSelected;
  const _ProductPickerSheet(
      {required this.products, required this.onSelected});

  @override
  State<_ProductPickerSheet> createState() => _ProductPickerSheetState();
}

class _ProductPickerSheetState extends State<_ProductPickerSheet> {
  late List<ProductWithPrice> _filtered;
  final _search = TextEditingController();

  @override
  void initState() {
    super.initState();
    _filtered = widget.products;
  }

  @override
  void dispose() {
    _search.dispose();
    super.dispose();
  }

  void _filter(String q) {
    final lower = q.toLowerCase();
    setState(() {
      _filtered = q.isEmpty
          ? widget.products
          : widget.products
              .where((p) =>
                  p.productCode.toLowerCase().contains(lower) ||
                  p.itemDescription.toLowerCase().contains(lower))
              .toList();
    });
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      height: MediaQuery.of(context).size.height * 0.78,
      decoration: BoxDecoration(
        color: const Color(0xFFF8F7F5),
        borderRadius: BorderRadius.vertical(top: Radius.circular(24.r)),
      ),
      child: Column(
        children: [
          // Sheet handle
          SizedBox(height: 10.h),
          Container(
            width: 36.w,
            height: 4.h,
            decoration: BoxDecoration(
              color: AppColors.foregroundMuted.withValues(alpha: 0.25),
              borderRadius: BorderRadius.circular(2.r),
            ),
          ),
          SizedBox(height: 16.h),

          // Header row
          Padding(
            padding: EdgeInsets.symmetric(horizontal: 20.w),
            child: Row(
              children: [
                Container(
                  width: 28.r,
                  height: 28.r,
                  decoration: BoxDecoration(
                    color: AppColors.primaryDark.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(7.r),
                  ),
                  child: Icon(Icons.storefront_outlined,
                      size: 14.r, color: AppColors.primaryDark),
                ),
                SizedBox(width: 8.w),
                Text(
                  'SELECT PRODUCT',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 13.sp,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 2.0,
                    color: AppColors.foregroundMuted,
                  ),
                ),
                const Spacer(),
                Text(
                  '${_filtered.length} results',
                  style: GoogleFonts.barlow(
                      fontSize: 11.sp,
                      color: AppColors.foregroundMuted),
                ),
              ],
            ),
          ),
          SizedBox(height: 12.h),

          // Search field
          Padding(
            padding: EdgeInsets.symmetric(horizontal: 16.w),
            child: Container(
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(12.r),
                boxShadow: [
                  BoxShadow(
                    color: const Color(0xFF1C1917).withValues(alpha: 0.05),
                    blurRadius: 10,
                    offset: const Offset(0, 2),
                  ),
                ],
              ),
              child: TextField(
                controller: _search,
                onChanged: _filter,
                style: GoogleFonts.barlow(
                    fontSize: 14.sp, color: AppColors.foreground),
                decoration: InputDecoration(
                  hintText: 'Search by code or description...',
                  hintStyle: GoogleFonts.barlow(
                      fontSize: 13.sp, color: AppColors.foregroundMuted),
                  prefixIcon: Icon(Icons.search_rounded,
                      size: 18.r,
                      color: AppColors.foregroundMuted),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12.r),
                    borderSide: BorderSide.none,
                  ),
                  enabledBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12.r),
                    borderSide: BorderSide.none,
                  ),
                  focusedBorder: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12.r),
                    borderSide: BorderSide(
                        color: AppColors.primary.withValues(alpha: 0.4),
                        width: 1.5),
                  ),
                  filled: true,
                  fillColor: Colors.white,
                  contentPadding:
                      EdgeInsets.symmetric(horizontal: 14.w, vertical: 13.h),
                ),
              ),
            ),
          ),
          SizedBox(height: 12.h),

          // Product list
          Expanded(
            child: _filtered.isEmpty
                ? Center(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.search_off_rounded,
                            size: 36.r,
                            color: AppColors.foregroundMuted
                                .withValues(alpha: 0.4)),
                        SizedBox(height: 8.h),
                        Text('No products found',
                            style: GoogleFonts.barlow(
                                fontSize: 14.sp,
                                color: AppColors.foregroundMuted)),
                      ],
                    ),
                  )
                : ListView.separated(
                    padding: EdgeInsets.symmetric(
                        horizontal: 16.w, vertical: 4.h),
                    itemCount: _filtered.length,
                    separatorBuilder: (_, __) => SizedBox(height: 6.h),
                    itemBuilder: (_, i) =>
                        _ProductRow(product: _filtered[i], onTap: () => widget.onSelected(_filtered[i])),
                  ),
          ),
          SizedBox(height: 16.h),
        ],
      ),
    );
  }
}

class _ProductRow extends StatelessWidget {
  final ProductWithPrice product;
  final VoidCallback onTap;
  const _ProductRow({required this.product, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return Material(
      color: Colors.white,
      borderRadius: BorderRadius.circular(12.r),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(12.r),
        child: Padding(
          padding: EdgeInsets.symmetric(horizontal: 14.w, vertical: 12.h),
          child: Row(
            children: [
              // Code badge
              Container(
                padding:
                    EdgeInsets.symmetric(horizontal: 8.w, vertical: 4.h),
                decoration: BoxDecoration(
                  color: AppColors.primary.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(6.r),
                ),
                child: Text(
                  product.productCode,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 10.sp,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 0.5,
                    color: AppColors.primary,
                  ),
                ),
              ),
              SizedBox(width: 12.w),
              // Description
              Expanded(
                child: Text(
                  product.itemDescription,
                  style: GoogleFonts.barlow(
                    fontSize: 13.sp,
                    fontWeight: FontWeight.w600,
                    color: AppColors.foreground,
                  ),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                ),
              ),
              SizedBox(width: 8.w),
              // Price
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    'Rs. ${product.unitPrice.toStringAsFixed(2)}',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w800,
                      letterSpacing: 0.3,
                      color: AppColors.foreground,
                    ),
                  ),
                  Text(
                    'per unit',
                    style: GoogleFonts.barlow(
                        fontSize: 9.sp,
                        color: AppColors.foregroundMuted),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Shared section header (mirrors detail page) ───────────────────────────────

class _SectionHeader extends StatelessWidget {
  final IconData icon;
  final String title;
  final String? badge;
  const _SectionHeader(
      {required this.icon, required this.title, this.badge});

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Container(
          width: 28.r,
          height: 28.r,
          decoration: BoxDecoration(
            color: AppColors.primaryDark.withValues(alpha: 0.1),
            borderRadius: BorderRadius.circular(7.r),
          ),
          child: Icon(icon, size: 14.r, color: AppColors.primaryDark),
        ),
        SizedBox(width: 8.w),
        Text(
          title,
          style: GoogleFonts.barlowCondensed(
            fontSize: 11.sp,
            fontWeight: FontWeight.w800,
            letterSpacing: 2.0,
            color: AppColors.foregroundMuted,
          ),
        ),
        if (badge != null) ...[
          SizedBox(width: 8.w),
          Container(
            padding:
                EdgeInsets.symmetric(horizontal: 7.w, vertical: 2.h),
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(20.r),
            ),
            child: Text(
              badge!,
              style: GoogleFonts.barlowCondensed(
                fontSize: 10.sp,
                fontWeight: FontWeight.w800,
                color: AppColors.primary,
              ),
            ),
          ),
        ],
      ],
    );
  }
}
