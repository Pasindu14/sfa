import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:get_it/get_it.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/core/widgets/app_spinner.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_summary.dart';
import 'package:uswatte/features/supervisor_billing/domain/usecases/get_supervisor_billings_usecase.dart';
import 'package:uswatte/features/supervisor_billing/presentation/bloc/supervisor_billing_bloc.dart';
import 'package:uswatte/features/supervisor_billing/presentation/bloc/supervisor_billing_event.dart';
import 'package:uswatte/features/supervisor_billing/presentation/bloc/supervisor_billing_state.dart';

class SupervisorBillingPage extends StatelessWidget {
  const SupervisorBillingPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => SupervisorBillingBloc(
        getMyReps: GetIt.instance<GetMyRepsUseCase>(),
        getSupervisorBillings:
            GetIt.instance<GetSupervisorBillingsUseCase>(),
      )..add(const LoadRepsRequested()),
      child: const _SupervisorBillingView(),
    );
  }
}

class _SupervisorBillingView extends StatelessWidget {
  const _SupervisorBillingView();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFFF5F4EE),
      body: Column(
        children: [
          _OrangeAppBar(),
          Expanded(
            child: BlocBuilder<SupervisorBillingBloc, SupervisorBillingState>(
              builder: (context, state) {
                if (state is SupervisorBillingInitial ||
                    state is SupervisorBillingLoadingReps) {
                  return const _LoadingBody();
                }
                if (state is SupervisorBillingLoadError) {
                  return _ErrorBody(message: state.message);
                }
                if (state is SupervisorBillingReady) {
                  return _ReadyBody(state: state);
                }
                return const SizedBox.shrink();
              },
            ),
          ),
        ],
      ),
    );
  }
}

// ── Orange app bar ────────────────────────────────────────────────────────────

class _OrangeAppBar extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return Container(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [AppColors.primaryDark, AppColors.primary],
        ),
      ),
      child: SafeArea(
        bottom: false,
        child: Stack(
          children: [
            Positioned(
              right: -18.w,
              top: -18.r,
              child: Container(
                width: 90.r,
                height: 90.r,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: Colors.white.withValues(alpha: 0.07),
                ),
              ),
            ),
            Padding(
              padding: EdgeInsets.symmetric(horizontal: 8.w, vertical: 10.r),
              child: Row(
                children: [
                  GestureDetector(
                    onTap: () => context.pop(),
                    child: Container(
                      width: 40.r,
                      height: 40.r,
                      margin: EdgeInsets.all(4.r),
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.15),
                        borderRadius: BorderRadius.circular(10.r),
                        border: Border.all(
                            color: Colors.white.withValues(alpha: 0.25)),
                      ),
                      child: Icon(Icons.arrow_back_ios_new_rounded,
                          size: 15.r, color: Colors.white),
                    ),
                  ),
                  SizedBox(width: 4.w),
                  Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'VIEW REP BILLS',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 18.sp,
                          fontWeight: FontWeight.w800,
                          letterSpacing: 1.5,
                          height: 1.0,
                          color: Colors.white,
                        ),
                      ),
                      SizedBox(height: 2.r),
                      Text(
                        'Bills created by your sales reps',
                        style: GoogleFonts.barlow(
                          fontSize: 11.sp,
                          color: Colors.white.withValues(alpha: 0.70),
                        ),
                      ),
                    ],
                  ),
                  const Spacer(),
                  Container(
                    width: 38.r,
                    height: 38.r,
                    margin: EdgeInsets.only(right: 16.w),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(10.r),
                    ),
                    child: Icon(Icons.receipt_long_rounded,
                        size: 18.r, color: Colors.white),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Loading body ──────────────────────────────────────────────────────────────

class _LoadingBody extends StatelessWidget {
  const _LoadingBody();

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const AppSpinner(),
        ],
      ),
    );
  }
}

// ── Error body ────────────────────────────────────────────────────────────────

class _ErrorBody extends StatelessWidget {
  final String message;
  const _ErrorBody({required this.message});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: EdgeInsets.symmetric(horizontal: 32.w),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 60.r,
              height: 60.r,
              decoration: BoxDecoration(
                color: AppColors.error.withValues(alpha: 0.08),
                shape: BoxShape.circle,
              ),
              child: Icon(Icons.error_outline_rounded,
                  size: 28.r, color: AppColors.error),
            ),
            SizedBox(height: 14.h),
            Text(message,
                textAlign: TextAlign.center,
                style: GoogleFonts.barlow(
                    fontSize: 14.sp,
                    color: AppColors.foregroundMuted,
                    height: 1.5)),
            SizedBox(height: 20.h),
            GestureDetector(
              onTap: () => context
                  .read<SupervisorBillingBloc>()
                  .add(const LoadRepsRequested()),
              child: Container(
                padding:
                    EdgeInsets.symmetric(horizontal: 24.w, vertical: 12.h),
                decoration: BoxDecoration(
                  color: AppColors.primary,
                  borderRadius: BorderRadius.circular(10.r),
                ),
                child: Text('Try Again',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 15.sp,
                      fontWeight: FontWeight.w700,
                      color: Colors.white,
                      letterSpacing: 0.5,
                    )),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Ready body ────────────────────────────────────────────────────────────────

class _ReadyBody extends StatelessWidget {
  final SupervisorBillingReady state;
  const _ReadyBody({required this.state});

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 40.h),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Step 1: Sales Rep ──────────────────────────────────────────────
          _StepCard(
            step: '01',
            label: 'SALES REP',
            icon: Icons.person_rounded,
            isComplete: state.selectedRep != null,
            child: _SearchableSelectField<RepSummary>(
              value: state.selectedRep,
              items: state.reps,
              labelBuilder: (r) => r.userName,
              placeholder: 'Select a sales rep...',
              sheetTitle: 'SELECT SALES REP',
              enabled: !state.isLoadingBillings,
              onChanged: (rep) {
                if (rep != null) {
                  context
                      .read<SupervisorBillingBloc>()
                      .add(RepSelected(rep));
                }
              },
            ),
          ),
          _StepConnector(),

          // ── Step 2: Date ───────────────────────────────────────────────────
          _StepCard(
            step: '02',
            label: 'DATE',
            icon: Icons.calendar_month_rounded,
            isComplete: true,
            child: _DatePickerField(
              selectedDate: state.selectedDate,
              enabled: !state.isLoadingBillings,
              onTap: () async {
                final picked = await showDatePicker(
                  context: context,
                  initialDate: state.selectedDate,
                  firstDate: DateTime.now().subtract(const Duration(days: 365)),
                  lastDate: DateTime.now(),
                  builder: (context, child) => Theme(
                    data: Theme.of(context).copyWith(
                      colorScheme: ColorScheme.light(
                        primary: AppColors.primary,
                        onPrimary: Colors.white,
                        surface: Colors.white,
                      ),
                    ),
                    child: child!,
                  ),
                );
                if (picked != null && context.mounted) {
                  context
                      .read<SupervisorBillingBloc>()
                      .add(DateSelected(picked));
                }
              },
            ),
          ),
          SizedBox(height: 24.h),

          // ── Error banner ───────────────────────────────────────────────────
          if (state.billingsError != null) ...[
            _ErrorBanner(message: state.billingsError!),
            SizedBox(height: 16.h),
          ],

          // ── Get Bills button ───────────────────────────────────────────────
          _GetBillsButton(
            canLoad: state.canLoad,
            isLoading: state.isLoadingBillings,
            onTap: () => context
                .read<SupervisorBillingBloc>()
                .add(const LoadBillingsRequested()),
          ),

          // ── Results ────────────────────────────────────────────────────────
          if (state.hasResults) ...[
            SizedBox(height: 28.h),
            _ResultsSection(
              billings: state.billings!,
              repName: state.selectedRep?.userName ?? '',
              date: state.selectedDate,
            ),
          ],
        ],
      ),
    );
  }
}

// ── Step card ─────────────────────────────────────────────────────────────────

class _StepCard extends StatelessWidget {
  final String step;
  final String label;
  final IconData icon;
  final bool isComplete;
  final Widget child;

  const _StepCard({
    required this.step,
    required this.label,
    required this.icon,
    required this.isComplete,
    required this.child,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16.r),
        boxShadow: [
          BoxShadow(
            color: const Color(0xFF1A1A11).withValues(alpha: 0.06),
            blurRadius: 16,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 12.h),
            decoration: const BoxDecoration(
              border: Border(
                bottom: BorderSide(color: Color(0xFFEEEDE6)),
              ),
            ),
            child: Row(
              children: [
                Container(
                  width: 28.r,
                  height: 28.r,
                  decoration: BoxDecoration(
                    color: isComplete
                        ? AppColors.primary
                        : const Color(0xFFEEEDE6),
                    borderRadius: BorderRadius.circular(8.r),
                  ),
                  child: Center(
                    child: Text(
                      step,
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 11.sp,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 0.5,
                        color: isComplete
                            ? Colors.white
                            : AppColors.foregroundMuted,
                      ),
                    ),
                  ),
                ),
                SizedBox(width: 10.w),
                Icon(icon,
                    size: 14.r,
                    color: isComplete
                        ? AppColors.primary
                        : AppColors.foregroundMuted),
                SizedBox(width: 6.w),
                Text(
                  label,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 2.0,
                    color: isComplete
                        ? AppColors.foreground
                        : AppColors.foregroundMuted,
                  ),
                ),
                const Spacer(),
                if (isComplete)
                  Container(
                    padding:
                        EdgeInsets.symmetric(horizontal: 8.w, vertical: 3.h),
                    decoration: BoxDecoration(
                      color: AppColors.primary.withValues(alpha: 0.10),
                      borderRadius: BorderRadius.circular(20.r),
                    ),
                    child: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.check_rounded,
                            size: 10.r, color: AppColors.primary),
                        SizedBox(width: 3.w),
                        Text(
                          'SET',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 9.sp,
                            fontWeight: FontWeight.w700,
                            letterSpacing: 1.0,
                            color: AppColors.primary,
                          ),
                        ),
                      ],
                    ),
                  ),
              ],
            ),
          ),
          Padding(
            padding: EdgeInsets.all(16.r),
            child: child,
          ),
        ],
      ),
    );
  }
}

// ── Step connector ────────────────────────────────────────────────────────────

class _StepConnector extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(left: 28.w),
      child: Column(
        children: List.generate(
          3,
          (i) => Container(
            width: 2.w,
            height: 5.h,
            margin: EdgeInsets.symmetric(vertical: 1.5.h),
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.25),
              borderRadius: BorderRadius.circular(1.r),
            ),
          ),
        ),
      ),
    );
  }
}

// ── Searchable select field ───────────────────────────────────────────────────

class _SearchableSelectField<T> extends StatelessWidget {
  final T? value;
  final List<T> items;
  final String Function(T) labelBuilder;
  final String placeholder;
  final String sheetTitle;
  final bool enabled;
  final ValueChanged<T?>? onChanged;

  const _SearchableSelectField({
    required this.value,
    required this.items,
    required this.labelBuilder,
    required this.placeholder,
    required this.sheetTitle,
    required this.enabled,
    required this.onChanged,
  });

  void _openSheet(BuildContext context) {
    showModalBottomSheet<T>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _SearchableSheetContent<T>(
        items: items,
        selectedValue: value,
        labelBuilder: labelBuilder,
        title: sheetTitle,
        onSelected: (item) {
          Navigator.of(context).pop();
          onChanged?.call(item);
        },
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final isSelected = value != null;
    return GestureDetector(
      onTap: enabled ? () => _openSheet(context) : null,
      child: Container(
        height: 50.h,
        padding: EdgeInsets.symmetric(horizontal: 14.w),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(10.r),
          border: Border.all(
            color: isSelected
                ? AppColors.primary.withValues(alpha: 0.40)
                : const Color(0xFFE0DFD9),
          ),
        ),
        child: Row(
          children: [
            Icon(
              isSelected
                  ? Icons.check_circle_outline_rounded
                  : Icons.search_rounded,
              size: 16.r,
              color:
                  isSelected ? AppColors.primary : AppColors.foregroundMuted,
            ),
            SizedBox(width: 10.w),
            Expanded(
              child: Text(
                isSelected ? labelBuilder(value as T) : placeholder,
                style: GoogleFonts.barlow(
                  fontSize: 14.sp,
                  fontWeight:
                      isSelected ? FontWeight.w600 : FontWeight.w400,
                  color: isSelected
                      ? AppColors.foreground
                      : AppColors.foregroundMuted,
                ),
                overflow: TextOverflow.ellipsis,
              ),
            ),
            Icon(Icons.keyboard_arrow_down_rounded,
                color:
                    enabled ? AppColors.primary : AppColors.foregroundMuted,
                size: 20.r),
          ],
        ),
      ),
    );
  }
}

// ── Searchable sheet content ──────────────────────────────────────────────────

class _SearchableSheetContent<T> extends StatefulWidget {
  final List<T> items;
  final T? selectedValue;
  final String Function(T) labelBuilder;
  final String title;
  final ValueChanged<T> onSelected;

  const _SearchableSheetContent({
    required this.items,
    required this.selectedValue,
    required this.labelBuilder,
    required this.title,
    required this.onSelected,
  });

  @override
  State<_SearchableSheetContent<T>> createState() =>
      _SearchableSheetContentState<T>();
}

class _SearchableSheetContentState<T>
    extends State<_SearchableSheetContent<T>> {
  final _controller = TextEditingController();
  late List<T> _filtered;

  @override
  void initState() {
    super.initState();
    _filtered = widget.items;
    _controller.addListener(_onSearch);
  }

  void _onSearch() {
    final q = _controller.text.toLowerCase();
    setState(() {
      _filtered = q.isEmpty
          ? widget.items
          : widget.items
              .where((i) =>
                  widget.labelBuilder(i).toLowerCase().contains(q))
              .toList();
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: 0.65,
      minChildSize: 0.4,
      maxChildSize: 0.92,
      expand: false,
      builder: (_, scrollController) => ClipRRect(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20.r)),
        child: Container(
          color: Colors.white,
          child: Column(
            children: [
              Container(
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                    colors: [AppColors.primaryDark, AppColors.primary],
                  ),
                ),
                child: Column(
                  children: [
                    SizedBox(height: 10.h),
                    Container(
                      width: 32.w,
                      height: 3.5.h,
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.35),
                        borderRadius: BorderRadius.circular(2.r),
                      ),
                    ),
                    SizedBox(height: 12.h),
                    Padding(
                      padding:
                          EdgeInsets.fromLTRB(20.w, 0, 16.w, 16.h),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  widget.title,
                                  style: GoogleFonts.barlowCondensed(
                                    fontSize: 19.sp,
                                    fontWeight: FontWeight.w800,
                                    letterSpacing: 1.2,
                                    color: Colors.white,
                                    height: 1.0,
                                  ),
                                ),
                                SizedBox(height: 2.h),
                                Text(
                                  '${widget.items.length} available',
                                  style: GoogleFonts.barlow(
                                    fontSize: 11.sp,
                                    color: Colors.white
                                        .withValues(alpha: 0.65),
                                  ),
                                ),
                              ],
                            ),
                          ),
                          GestureDetector(
                            onTap: () => Navigator.of(context).pop(),
                            child: Container(
                              width: 32.r,
                              height: 32.r,
                              decoration: BoxDecoration(
                                color:
                                    Colors.white.withValues(alpha: 0.18),
                                shape: BoxShape.circle,
                              ),
                              child: Icon(Icons.close_rounded,
                                  size: 15.r, color: Colors.white),
                            ),
                          ),
                        ],
                      ),
                    ),
                    Padding(
                      padding:
                          EdgeInsets.fromLTRB(16.w, 0, 16.w, 14.h),
                      child: Container(
                        height: 52.h,
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(10.r),
                        ),
                        child: Row(
                          children: [
                            Padding(
                              padding:
                                  EdgeInsets.symmetric(horizontal: 12.w),
                              child: Icon(Icons.search_rounded,
                                  size: 17.r,
                                  color: const Color(0xFFADADA5)),
                            ),
                            Expanded(
                              child: TextField(
                                controller: _controller,
                                cursorColor: AppColors.primary,
                                style: GoogleFonts.barlow(
                                    fontSize: 14.sp,
                                    color: AppColors.foreground),
                                decoration: InputDecoration(
                                  hintText: 'Search...',
                                  hintStyle: GoogleFonts.barlow(
                                      fontSize: 14.sp,
                                      color: const Color(0xFFADADA5)),
                                  border: InputBorder.none,
                                  enabledBorder: InputBorder.none,
                                  focusedBorder: InputBorder.none,
                                  filled: true,
                                  fillColor: Colors.white,
                                  isDense: true,
                                  contentPadding: EdgeInsets.zero,
                                ),
                              ),
                            ),
                            if (_controller.text.isNotEmpty)
                              GestureDetector(
                                onTap: () => _controller.clear(),
                                child: Padding(
                                  padding: EdgeInsets.symmetric(
                                      horizontal: 10.w),
                                  child: Icon(Icons.cancel_rounded,
                                      size: 16.r,
                                      color:
                                          const Color(0xFFADADA5)),
                                ),
                              )
                            else
                              SizedBox(width: 12.w),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              Container(
                color: const Color(0xFFF8F8F5),
                padding:
                    EdgeInsets.symmetric(horizontal: 20.w, vertical: 8.h),
                child: Row(
                  children: [
                    Text(
                      _controller.text.isEmpty
                          ? 'All results'
                          : '${_filtered.length} result${_filtered.length == 1 ? '' : 's'}',
                      style: GoogleFonts.barlow(
                        fontSize: 11.sp,
                        fontWeight: FontWeight.w600,
                        color: const Color(0xFFADADA5),
                        letterSpacing: 0.3,
                      ),
                    ),
                  ],
                ),
              ),
              Divider(height: 1, color: const Color(0xFFF0EFEA)),
              Expanded(
                child: _filtered.isEmpty
                    ? Center(
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.search_off_rounded,
                                size: 34.r,
                                color: const Color(0xFFCCCCC4)),
                            SizedBox(height: 10.h),
                            Text('No results',
                                style: GoogleFonts.barlow(
                                    fontSize: 14.sp,
                                    fontWeight: FontWeight.w500,
                                    color: const Color(0xFFADADA5))),
                            SizedBox(height: 2.h),
                            Text('Try a different search term',
                                style: GoogleFonts.barlow(
                                    fontSize: 12.sp,
                                    color: const Color(0xFFCCCCC4))),
                          ],
                        ),
                      )
                    : ListView.builder(
                        controller: scrollController,
                        padding: EdgeInsets.only(bottom: 24.h),
                        itemCount: _filtered.length,
                        itemBuilder: (_, index) {
                          final item = _filtered[index];
                          final isActive = item == widget.selectedValue;
                          return _SheetListTile<T>(
                            item: item,
                            isActive: isActive,
                            labelBuilder: widget.labelBuilder,
                            onTap: () => widget.onSelected(item),
                          );
                        },
                      ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Sheet list tile ───────────────────────────────────────────────────────────

class _SheetListTile<T> extends StatefulWidget {
  final T item;
  final bool isActive;
  final String Function(T) labelBuilder;
  final VoidCallback onTap;

  const _SheetListTile({
    required this.item,
    required this.isActive,
    required this.labelBuilder,
    required this.onTap,
  });

  @override
  State<_SheetListTile<T>> createState() => _SheetListTileState<T>();
}

class _SheetListTileState<T> extends State<_SheetListTile<T>> {
  bool _pressed = false;

  @override
  Widget build(BuildContext context) {
    final label = widget.labelBuilder(widget.item);
    final initial = label.isNotEmpty ? label[0].toUpperCase() : '?';

    return GestureDetector(
      onTap: widget.onTap,
      onTapDown: (_) => setState(() => _pressed = true),
      onTapUp: (_) => setState(() => _pressed = false),
      onTapCancel: () => setState(() => _pressed = false),
      behavior: HitTestBehavior.opaque,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 80),
        color: _pressed
            ? const Color(0xFFF5F4EE)
            : widget.isActive
                ? AppColors.primary.withValues(alpha: 0.05)
                : Colors.white,
        padding: EdgeInsets.symmetric(horizontal: 20.w, vertical: 12.h),
        child: Row(
          children: [
            Container(
              width: 38.r,
              height: 38.r,
              decoration: BoxDecoration(
                color: widget.isActive
                    ? AppColors.primary
                    : const Color(0xFFF0EFEA),
                shape: BoxShape.circle,
              ),
              child: Center(
                child: Text(
                  initial,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 16.sp,
                    fontWeight: FontWeight.w800,
                    color: widget.isActive
                        ? Colors.white
                        : const Color(0xFF8A8A82),
                  ),
                ),
              ),
            ),
            SizedBox(width: 14.w),
            Expanded(
              child: Text(
                label,
                style: GoogleFonts.barlow(
                  fontSize: 14.sp,
                  fontWeight:
                      widget.isActive ? FontWeight.w700 : FontWeight.w500,
                  color: widget.isActive
                      ? AppColors.primary
                      : AppColors.foreground,
                ),
                overflow: TextOverflow.ellipsis,
              ),
            ),
            if (widget.isActive)
              Icon(Icons.check_rounded,
                  size: 18.r, color: AppColors.primary)
            else
              SizedBox(width: 18.r),
          ],
        ),
      ),
    );
  }
}

// ── Date picker field ─────────────────────────────────────────────────────────

class _DatePickerField extends StatelessWidget {
  final DateTime selectedDate;
  final bool enabled;
  final VoidCallback onTap;

  const _DatePickerField({
    required this.selectedDate,
    required this.enabled,
    required this.onTap,
  });

  String _format(DateTime dt) {
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    const days = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    return '${days[dt.weekday - 1]}, ${months[dt.month - 1]} ${dt.day}, ${dt.year}';
  }

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: enabled ? onTap : null,
      child: Container(
        height: 50.h,
        padding: EdgeInsets.symmetric(horizontal: 14.w),
        decoration: BoxDecoration(
          color: AppColors.primary.withValues(alpha: 0.05),
          borderRadius: BorderRadius.circular(10.r),
          border: Border.all(
              color: AppColors.primary.withValues(alpha: 0.30)),
        ),
        child: Row(
          children: [
            Icon(Icons.calendar_today_rounded,
                size: 15.r, color: AppColors.primary),
            SizedBox(width: 10.w),
            Expanded(
              child: Text(
                _format(selectedDate),
                style: GoogleFonts.barlow(
                  fontSize: 14.sp,
                  fontWeight: FontWeight.w600,
                  color: AppColors.foreground,
                ),
              ),
            ),
            Icon(Icons.chevron_right_rounded,
                color: AppColors.foregroundMuted, size: 18.r),
          ],
        ),
      ),
    );
  }
}

// ── Error banner ──────────────────────────────────────────────────────────────

class _ErrorBanner extends StatelessWidget {
  final String message;
  const _ErrorBanner({required this.message});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.all(14.r),
      decoration: BoxDecoration(
        color: AppColors.error.withValues(alpha: 0.06),
        borderRadius: BorderRadius.circular(12.r),
        border:
            Border.all(color: AppColors.error.withValues(alpha: 0.25)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 28.r,
            height: 28.r,
            decoration: BoxDecoration(
              color: AppColors.error.withValues(alpha: 0.10),
              borderRadius: BorderRadius.circular(8.r),
            ),
            child: Icon(Icons.error_outline_rounded,
                color: AppColors.error, size: 15.r),
          ),
          SizedBox(width: 12.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Load Failed',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 13.sp,
                      fontWeight: FontWeight.w700,
                      color: AppColors.error,
                      letterSpacing: 0.3,
                    )),
                SizedBox(height: 2.h),
                Text(message,
                    style: GoogleFonts.barlow(
                        fontSize: 12.sp,
                        color: AppColors.foreground,
                        height: 1.4)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Get Bills button ──────────────────────────────────────────────────────────

class _GetBillsButton extends StatelessWidget {
  final bool canLoad;
  final bool isLoading;
  final VoidCallback onTap;

  const _GetBillsButton({
    required this.canLoad,
    required this.isLoading,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: canLoad ? onTap : null,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        width: double.infinity,
        height: 56.h,
        decoration: BoxDecoration(
          gradient: canLoad
              ? LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [AppColors.primaryDark, AppColors.primary],
                )
              : null,
          color: canLoad ? null : const Color(0xFFEEEDE6),
          borderRadius: BorderRadius.circular(14.r),
          boxShadow: canLoad
              ? [
                  BoxShadow(
                    color: AppColors.primary.withValues(alpha: 0.35),
                    blurRadius: 20,
                    offset: const Offset(0, 6),
                  ),
                ]
              : null,
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            if (isLoading)
              const AppSpinner.button()
            else
              Icon(
                Icons.search_rounded,
                size: 18.r,
                color: canLoad ? Colors.white : AppColors.foregroundMuted,
              ),
            SizedBox(width: 10.w),
            Text(
              'GET BILLS',
              style: GoogleFonts.barlowCondensed(
                fontSize: 16.sp,
                fontWeight: FontWeight.w800,
                letterSpacing: 1.5,
                color: canLoad ? Colors.white : AppColors.foregroundMuted,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Results section ───────────────────────────────────────────────────────────

class _ResultsSection extends StatelessWidget {
  final List<BillingSummary> billings;
  final String repName;
  final DateTime date;

  const _ResultsSection({
    required this.billings,
    required this.repName,
    required this.date,
  });

  String _formatDate(DateTime dt) {
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    return '${months[dt.month - 1]} ${dt.day}, ${dt.year}';
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Section header
        Row(
          children: [
            Container(
              width: 3.w,
              height: 13.h,
              decoration: BoxDecoration(
                color: AppColors.primary,
                borderRadius: BorderRadius.circular(2.r),
              ),
            ),
            SizedBox(width: 8.w),
            Expanded(
              child: Text(
                '${billings.length} BILL${billings.length == 1 ? '' : 'S'} · ${_formatDate(date)}',
                style: GoogleFonts.barlowCondensed(
                  fontSize: 11.sp,
                  fontWeight: FontWeight.w700,
                  letterSpacing: 2.5,
                  color: AppColors.foregroundMuted,
                ),
              ),
            ),
          ],
        ),
        SizedBox(height: 12.h),

        if (billings.isEmpty)
          _EmptyBillsState(repName: repName, date: date)
        else
          ...billings.map((b) => Padding(
                padding: EdgeInsets.only(bottom: 10.h),
                child: _BillingCard(
                  billing: b,
                  onTap: () => context.push(
                    '/supervisor/billing/${b.id}',
                    extra: b.billingNumber,
                  ),
                ),
              )),
      ],
    );
  }
}

// ── Empty bills state ─────────────────────────────────────────────────────────

class _EmptyBillsState extends StatelessWidget {
  final String repName;
  final DateTime date;

  const _EmptyBillsState({required this.repName, required this.date});

  String _formatDate(DateTime dt) {
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    return '${months[dt.month - 1]} ${dt.day}, ${dt.year}';
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: EdgeInsets.symmetric(vertical: 32.h),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16.r),
        border: Border.all(color: const Color(0xFFEEEDE6)),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 52.r,
            height: 52.r,
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.07),
              shape: BoxShape.circle,
            ),
            child: Icon(Icons.receipt_long_rounded,
                size: 24.r,
                color: AppColors.primary.withValues(alpha: 0.5)),
          ),
          SizedBox(height: 12.h),
          Text('No bills found',
              style: GoogleFonts.barlowCondensed(
                fontSize: 16.sp,
                fontWeight: FontWeight.w700,
                color: AppColors.foreground,
              )),
          SizedBox(height: 4.h),
          Text(
            '$repName had no bills on ${_formatDate(date)}',
            textAlign: TextAlign.center,
            style: GoogleFonts.barlow(
                fontSize: 12.sp,
                color: AppColors.foregroundMuted,
                height: 1.4),
          ),
        ],
      ),
    );
  }
}

// ── Billing card ──────────────────────────────────────────────────────────────

class _BillingCard extends StatefulWidget {
  final BillingSummary billing;
  final VoidCallback onTap;

  const _BillingCard({required this.billing, required this.onTap});

  @override
  State<_BillingCard> createState() => _BillingCardState();
}

class _BillingCardState extends State<_BillingCard> {
  bool _pressed = false;

  String _formatAmount(double amount) {
    return 'LKR ${amount.toStringAsFixed(2).replaceAllMapped(RegExp(r'(\d)(?=(\d{3})+\.)'), (m) => '${m[1]},')}';
  }

  Color get _statusColor {
    switch (widget.billing.status) {
      case BillingStatus.approved:
        return AppColors.success;
      case BillingStatus.cancelled:
        return AppColors.error;
      case BillingStatus.submitted:
        return AppColors.warning;
    }
  }

  String get _statusLabel {
    switch (widget.billing.status) {
      case BillingStatus.approved:
        return 'Approved';
      case BillingStatus.cancelled:
        return 'Cancelled';
      case BillingStatus.submitted:
        return 'Submitted';
    }
  }

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: widget.onTap,
      onTapDown: (_) => setState(() => _pressed = true),
      onTapUp: (_) => setState(() => _pressed = false),
      onTapCancel: () => setState(() => _pressed = false),
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 80),
        decoration: BoxDecoration(
          color: _pressed ? const Color(0xFFF8F7F2) : Colors.white,
          borderRadius: BorderRadius.circular(14.r),
          boxShadow: [
            BoxShadow(
              color: const Color(0xFF1A1A11).withValues(alpha: 0.05),
              blurRadius: 12,
              offset: const Offset(0, 3),
            ),
          ],
        ),
        child: Padding(
          padding: EdgeInsets.all(16.r),
          child: Row(
            children: [
              // Bill icon
              Container(
                width: 42.r,
                height: 42.r,
                decoration: BoxDecoration(
                  color: AppColors.primary.withValues(alpha: 0.08),
                  borderRadius: BorderRadius.circular(10.r),
                ),
                child: Icon(Icons.receipt_rounded,
                    size: 20.r, color: AppColors.primary),
              ),
              SizedBox(width: 14.w),
              // Bill info
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Text(
                          widget.billing.billingNumber,
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 15.sp,
                            fontWeight: FontWeight.w800,
                            letterSpacing: 0.3,
                            color: AppColors.foreground,
                          ),
                        ),
                        const Spacer(),
                        Container(
                          padding: EdgeInsets.symmetric(
                              horizontal: 8.w, vertical: 3.h),
                          decoration: BoxDecoration(
                            color: _statusColor.withValues(alpha: 0.10),
                            borderRadius: BorderRadius.circular(20.r),
                          ),
                          child: Text(
                            _statusLabel,
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 10.sp,
                              fontWeight: FontWeight.w700,
                              letterSpacing: 0.5,
                              color: _statusColor,
                            ),
                          ),
                        ),
                      ],
                    ),
                    SizedBox(height: 3.h),
                    Text(
                      widget.billing.outletName,
                      style: GoogleFonts.barlow(
                        fontSize: 12.sp,
                        color: AppColors.foregroundMuted,
                      ),
                      overflow: TextOverflow.ellipsis,
                    ),
                    SizedBox(height: 6.h),
                    Row(
                      children: [
                        Text(
                          _formatAmount(widget.billing.totalAmount),
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 14.sp,
                            fontWeight: FontWeight.w700,
                            color: AppColors.foreground,
                          ),
                        ),
                        const Spacer(),
                        Icon(Icons.arrow_forward_ios_rounded,
                            size: 11.r,
                            color: AppColors.foregroundMuted),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
