import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:get_it/get_it.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_route.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/create_assignment_usecase.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_rep_routes_usecase.dart';
import 'package:uswatte/features/route_assignment/presentation/bloc/route_assignment_bloc.dart';

class RouteAssignmentPage extends StatelessWidget {
  const RouteAssignmentPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider(
      create: (_) => RouteAssignmentBloc(
        getMyReps: GetIt.instance<GetMyRepsUseCase>(),
        getRepRoutes: GetIt.instance<GetRepRoutesUseCase>(),
        createAssignment: GetIt.instance<CreateAssignmentUseCase>(),
      )..add(const LoadRepsRequested()),
      child: const _RouteAssignmentView(),
    );
  }
}

class _RouteAssignmentView extends StatelessWidget {
  const _RouteAssignmentView();

  @override
  Widget build(BuildContext context) {
    return BlocListener<RouteAssignmentBloc, RouteAssignmentState>(
      listener: (context, state) {
        if (state is RouteAssignmentSuccess) {
          ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(
              content: Row(
                children: [
                  Icon(Icons.check_circle_rounded,
                      color: Colors.white, size: 18.r),
                  SizedBox(width: 10.w),
                  Text(
                    'Route assigned successfully.',
                    style: GoogleFonts.barlow(
                        fontSize: 14.sp, color: Colors.white),
                  ),
                ],
              ),
              backgroundColor: AppColors.success,
              behavior: SnackBarBehavior.floating,
              margin: EdgeInsets.all(16.r),
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12.r)),
            ),
          );
          context.go('/supervisor/home');
        }
      },
      child: Scaffold(
        backgroundColor: const Color(0xFFF5F4EE),
        body: Column(
          children: [
            _OrangeAppBar(),
            Expanded(
              child: BlocBuilder<RouteAssignmentBloc, RouteAssignmentState>(
                builder: (context, state) {
                  if (state is RouteAssignmentInitial ||
                      state is RouteAssignmentLoadingReps) {
                    return const _LoadingBody();
                  }

                  if (state is RouteAssignmentLoadError) {
                    return _ErrorBody(message: state.message);
                  }

                  if (state is RouteAssignmentLoadingRoutes) {
                    return _FormBody(
                      reps: state.reps,
                      selectedRep: state.selectedRep,
                      routes: const [],
                      selectedRoute: null,
                      selectedDate: null,
                      isLoadingRoutes: true,
                      isSaving: false,
                      errorMessage: null,
                    );
                  }

                  RouteAssignmentReady? ready;
                  bool isSaving = false;
                  String? errorMessage;

                  if (state is RouteAssignmentReady) {
                    ready = state;
                  } else if (state is RouteAssignmentSaving) {
                    ready = state.formState;
                    isSaving = true;
                  } else if (state is RouteAssignmentError) {
                    ready = state.formState;
                    errorMessage = state.message;
                  }

                  if (ready == null) return const SizedBox.shrink();

                  return _FormBody(
                    reps: ready.reps,
                    selectedRep: ready.selectedRep,
                    routes: ready.routes,
                    selectedRoute: ready.selectedRoute,
                    selectedDate: ready.selectedDate,
                    isLoadingRoutes: false,
                    isSaving: isSaving,
                    errorMessage: errorMessage,
                    canSubmit: ready.canSubmit,
                  );
                },
              ),
            ),
          ],
        ),
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
            // Decorative circles (clipped by overflow on Stack)
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
            // Content row — no fixed height, grows with content
            Padding(
              padding:
                  EdgeInsets.symmetric(horizontal: 8.w, vertical: 10.r),
              child: Row(
                children: [
                  // Back button
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
                  // Title block
                  Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'ASSIGN DAILY ROUTE',
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
                        'Schedule a rep\'s route',
                        style: GoogleFonts.barlow(
                          fontSize: 11.sp,
                          color: Colors.white.withValues(alpha: 0.70),
                        ),
                      ),
                    ],
                  ),
                  const Spacer(),
                  // Route icon badge
                  Container(
                    width: 38.r,
                    height: 38.r,
                    margin: EdgeInsets.only(right: 16.w),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.15),
                      borderRadius: BorderRadius.circular(10.r),
                    ),
                    child: Icon(Icons.alt_route_rounded,
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
          Container(
            width: 56.r,
            height: 56.r,
            decoration: BoxDecoration(
              color: AppColors.primary.withValues(alpha: 0.08),
              shape: BoxShape.circle,
            ),
            child: Padding(
              padding: EdgeInsets.all(14.r),
              child: CircularProgressIndicator(
                  color: AppColors.primary, strokeWidth: 2.5),
            ),
          ),
          SizedBox(height: 16.h),
          Text('Loading reps...',
              style: GoogleFonts.barlow(
                  fontSize: 14.sp, color: AppColors.foregroundMuted)),
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
              child:
                  Icon(Icons.error_outline_rounded, size: 28.r, color: AppColors.error),
            ),
            SizedBox(height: 14.h),
            Text(message,
                textAlign: TextAlign.center,
                style: GoogleFonts.barlow(
                    fontSize: 14.sp, color: AppColors.foregroundMuted, height: 1.5)),
            SizedBox(height: 20.h),
            GestureDetector(
              onTap: () => context
                  .read<RouteAssignmentBloc>()
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

// ── Form body ─────────────────────────────────────────────────────────────────
class _FormBody extends StatelessWidget {
  final List<RepSummary> reps;
  final RepSummary selectedRep;
  final List<RepRoute> routes;
  final RepRoute? selectedRoute;
  final DateTime? selectedDate;
  final bool isLoadingRoutes;
  final bool isSaving;
  final String? errorMessage;
  final bool canSubmit;

  const _FormBody({
    required this.reps,
    required this.selectedRep,
    required this.routes,
    required this.selectedRoute,
    required this.selectedDate,
    required this.isLoadingRoutes,
    required this.isSaving,
    required this.errorMessage,
    this.canSubmit = false,
  });

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(16.w, 20.h, 16.w, 40.h),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Step 1: Sales Rep ────────────────────────────────────────────
          _StepCard(
            step: '01',
            label: 'SALES REP',
            icon: Icons.person_rounded,
            isComplete: true,
            child: _SearchableSelectField<RepSummary>(
              value: selectedRep,
              items: reps,
              labelBuilder: (r) => r.userName,
              placeholder: 'Select a sales rep...',
              sheetTitle: 'SELECT SALES REP',
              enabled: !isSaving,
              onChanged: (rep) {
                if (rep != null) {
                  context.read<RouteAssignmentBloc>().add(RepSelected(rep));
                }
              },
            ),
          ),
          _StepConnector(),

          // ── Step 2: Route ────────────────────────────────────────────────
          _StepCard(
            step: '02',
            label: 'ROUTE',
            icon: Icons.alt_route_rounded,
            isComplete: selectedRoute != null,
            child: isLoadingRoutes
                ? _RouteLoadingIndicator()
                : routes.isEmpty
                    ? _NoRoutesWarning()
                    : _SearchableSelectField<RepRoute>(
                        value: selectedRoute,
                        items: routes,
                        labelBuilder: (r) => r.routeName,
                        placeholder: 'Select a route...',
                        sheetTitle: 'SELECT ROUTE',
                        enabled: !isSaving,
                        onChanged: (route) {
                          if (route != null) {
                            context
                                .read<RouteAssignmentBloc>()
                                .add(RouteSelected(route));
                          }
                        },
                      ),
          ),
          _StepConnector(),

          // ── Step 3: Date ─────────────────────────────────────────────────
          _StepCard(
            step: '03',
            label: 'DATE',
            icon: Icons.calendar_month_rounded,
            isComplete: selectedDate != null,
            child: _DatePickerField(
              selectedDate: selectedDate,
              enabled: !isSaving && !isLoadingRoutes,
              onTap: () async {
                final picked = await showDatePicker(
                  context: context,
                  initialDate: DateTime.now(),
                  firstDate: DateTime.now(),
                  lastDate: DateTime.now().add(const Duration(days: 90)),
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
                      .read<RouteAssignmentBloc>()
                      .add(DateSelected(picked));
                }
              },
            ),
          ),
          SizedBox(height: 24.h),

          // ── Error banner ─────────────────────────────────────────────────
          if (errorMessage != null) ...[
            _ErrorBanner(message: errorMessage!),
            SizedBox(height: 16.h),
          ],

          // ── Submit button ─────────────────────────────────────────────────
          _AssignButton(
            canSubmit: canSubmit && !isSaving && !isLoadingRoutes,
            isSaving: isSaving,
            onTap: () => context
                .read<RouteAssignmentBloc>()
                .add(const AssignmentSubmitted()),
          ),
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
          // Card header
          Container(
            padding: EdgeInsets.fromLTRB(16.w, 14.h, 16.w, 12.h),
            decoration: BoxDecoration(
              border: Border(
                bottom: BorderSide(color: const Color(0xFFEEEDE6)),
              ),
            ),
            child: Row(
              children: [
                // Step number badge
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
          // Card content
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
              isSelected ? Icons.check_circle_outline_rounded : Icons.search_rounded,
              size: 16.r,
              color: isSelected ? AppColors.primary : AppColors.foregroundMuted,
            ),
            SizedBox(width: 10.w),
            Expanded(
              child: Text(
                isSelected ? labelBuilder(value as T) : placeholder,
                style: GoogleFonts.barlow(
                  fontSize: 14.sp,
                  fontWeight: isSelected ? FontWeight.w600 : FontWeight.w400,
                  color: isSelected ? AppColors.foreground : AppColors.foregroundMuted,
                ),
                overflow: TextOverflow.ellipsis,
              ),
            ),
            Icon(Icons.keyboard_arrow_down_rounded,
                color: enabled ? AppColors.primary : AppColors.foregroundMuted,
                size: 20.r),
          ],
        ),
      ),
    );
  }
}

// ── Searchable bottom sheet content ──────────────────────────────────────────
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
              .where((i) => widget.labelBuilder(i).toLowerCase().contains(q))
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
              // ── Orange header with handle ─────────────────────────────────
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
                    // Handle on orange bg
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
                    // Title row
                    Padding(
                      padding: EdgeInsets.fromLTRB(20.w, 0, 16.w, 16.h),
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
                                    color: Colors.white.withValues(alpha: 0.65),
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
                                color: Colors.white.withValues(alpha: 0.18),
                                shape: BoxShape.circle,
                              ),
                              child: Icon(Icons.close_rounded,
                                  size: 15.r, color: Colors.white),
                            ),
                          ),
                        ],
                      ),
                    ),
                    // Search bar embedded in orange header
                    Padding(
                      padding: EdgeInsets.fromLTRB(16.w, 0, 16.w, 14.h),
                      child: Container(
                        height: 52.h,
                        decoration: BoxDecoration(
                          color: Colors.white,
                          borderRadius: BorderRadius.circular(10.r),
                        ),
                        child: Row(
                          children: [
                            Padding(
                              padding: EdgeInsets.symmetric(horizontal: 12.w),
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
                                  padding:
                                      EdgeInsets.symmetric(horizontal: 10.w),
                                  child: Icon(Icons.cancel_rounded,
                                      size: 16.r,
                                      color: const Color(0xFFADADA5)),
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
              // ── Results count bar ─────────────────────────────────────────
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
              // ── List ──────────────────────────────────────────────────────
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
            // Avatar
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
            // Label
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
            // Check
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

// ── Route loading indicator ───────────────────────────────────────────────────
class _RouteLoadingIndicator extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      height: 50.h,
      padding: EdgeInsets.symmetric(horizontal: 14.w),
      decoration: BoxDecoration(
        color: const Color(0xFFF6F5EF),
        borderRadius: BorderRadius.circular(10.r),
        border: Border.all(color: const Color(0xFFEEEDE6)),
      ),
      child: Row(
        children: [
          SizedBox(
            width: 15.r,
            height: 15.r,
            child: CircularProgressIndicator(
                strokeWidth: 2, color: AppColors.primary),
          ),
          SizedBox(width: 12.w),
          Text('Loading routes...',
              style: GoogleFonts.barlow(
                  fontSize: 14.sp, color: AppColors.foregroundMuted)),
        ],
      ),
    );
  }
}

// ── No routes warning ─────────────────────────────────────────────────────────
class _NoRoutesWarning extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.all(12.r),
      decoration: BoxDecoration(
        color: AppColors.warning.withValues(alpha: 0.07),
        borderRadius: BorderRadius.circular(10.r),
        border: Border.all(color: AppColors.warning.withValues(alpha: 0.25)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(Icons.warning_amber_rounded,
              color: AppColors.warning, size: 16.r),
          SizedBox(width: 8.w),
          Expanded(
            child: Text(
              'No routes found for this rep. Ask an admin to configure their geo assignment first.',
              style: GoogleFonts.barlow(
                  fontSize: 12.sp, color: AppColors.foreground, height: 1.45),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Date picker field ─────────────────────────────────────────────────────────
class _DatePickerField extends StatelessWidget {
  final DateTime? selectedDate;
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
          color: selectedDate != null
              ? AppColors.primary.withValues(alpha: 0.05)
              : const Color(0xFFF6F5EF),
          borderRadius: BorderRadius.circular(10.r),
          border: Border.all(
            color: selectedDate != null
                ? AppColors.primary.withValues(alpha: 0.30)
                : const Color(0xFFEEEDE6),
          ),
        ),
        child: Row(
          children: [
            Icon(Icons.calendar_today_rounded,
                size: 15.r,
                color: selectedDate != null
                    ? AppColors.primary
                    : AppColors.foregroundMuted),
            SizedBox(width: 10.w),
            Expanded(
              child: Text(
                selectedDate != null ? _format(selectedDate!) : 'Tap to select a date',
                style: GoogleFonts.barlow(
                  fontSize: 14.sp,
                  fontWeight:
                      selectedDate != null ? FontWeight.w600 : FontWeight.w400,
                  color: selectedDate != null
                      ? AppColors.foreground
                      : AppColors.foregroundMuted,
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
        border: Border.all(color: AppColors.error.withValues(alpha: 0.25)),
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
                Text('Assignment Failed',
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

// ── Assign button ─────────────────────────────────────────────────────────────
class _AssignButton extends StatelessWidget {
  final bool canSubmit;
  final bool isSaving;
  final VoidCallback onTap;

  const _AssignButton({
    required this.canSubmit,
    required this.isSaving,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: canSubmit ? onTap : null,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 200),
        width: double.infinity,
        height: 56.h,
        decoration: BoxDecoration(
          gradient: canSubmit
              ? LinearGradient(
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                  colors: [AppColors.primaryDark, AppColors.primary],
                )
              : null,
          color: canSubmit ? null : const Color(0xFFEEEDE6),
          borderRadius: BorderRadius.circular(14.r),
          boxShadow: canSubmit
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
            if (isSaving)
              SizedBox(
                width: 18.r,
                height: 18.r,
                child: const CircularProgressIndicator(
                    color: Colors.white, strokeWidth: 2.5),
              )
            else
              Icon(
                Icons.check_circle_outline_rounded,
                size: 18.r,
                color: canSubmit ? Colors.white : AppColors.foregroundMuted,
              ),
            SizedBox(width: 10.w),
            Text(
              isSaving ? 'ASSIGNING...' : 'CONFIRM ASSIGNMENT',
              style: GoogleFonts.barlowCondensed(
                fontSize: 16.sp,
                fontWeight: FontWeight.w800,
                letterSpacing: 1.5,
                color: canSubmit ? Colors.white : AppColors.foregroundMuted,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
