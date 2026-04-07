import 'package:flutter/material.dart';
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
              content: Text(
                'Route assigned successfully.',
                style: GoogleFonts.barlow(fontSize: 14.sp),
              ),
              backgroundColor: AppColors.success,
              behavior: SnackBarBehavior.floating,
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(10.r)),
            ),
          );
          context.go('/supervisor/home');
        }
        if (state is RouteAssignmentError) {
          // Error is displayed inline — no snackbar
        }
      },
      child: Scaffold(
        backgroundColor: AppColors.background,
        appBar: AppBar(
          backgroundColor: AppColors.background,
          surfaceTintColor: Colors.transparent,
          elevation: 0,
          leading: GestureDetector(
            onTap: () => context.go('/supervisor/home'),
            child: Container(
              margin: EdgeInsets.all(10.r),
              decoration: BoxDecoration(
                color: AppColors.surface,
                borderRadius: BorderRadius.circular(8.r),
                border: Border.all(color: AppColors.surfaceVariant),
              ),
              child: Icon(Icons.arrow_back_ios_new_rounded,
                  size: 14.r, color: AppColors.foreground),
            ),
          ),
          title: Text(
            'Assign Daily Route',
            style: GoogleFonts.barlowCondensed(
              fontSize: 20.sp,
              fontWeight: FontWeight.w700,
              letterSpacing: 0.5,
              color: AppColors.foreground,
            ),
          ),
        ),
        body: BlocBuilder<RouteAssignmentBloc, RouteAssignmentState>(
          builder: (context, state) {
            if (state is RouteAssignmentInitial ||
                state is RouteAssignmentLoadingReps) {
              return const _LoadingBody(message: 'Loading reps...');
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
    );
  }
}

// ── Loading body ──────────────────────────────────────────────────────────────
class _LoadingBody extends StatelessWidget {
  final String message;
  const _LoadingBody({required this.message});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          CircularProgressIndicator(color: AppColors.primary, strokeWidth: 2.5),
          SizedBox(height: 16.h),
          Text(message,
              style: GoogleFonts.barlow(
                  fontSize: 14.sp, color: AppColors.foregroundMuted)),
        ],
      ),
    );
  }
}

// ── Error body (load failure) ─────────────────────────────────────────────────
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
            Icon(Icons.error_outline_rounded,
                size: 48.r, color: AppColors.error),
            SizedBox(height: 12.h),
            Text(message,
                textAlign: TextAlign.center,
                style: GoogleFonts.barlow(
                    fontSize: 14.sp, color: AppColors.foregroundMuted)),
            SizedBox(height: 20.h),
            OutlinedButton(
              onPressed: () => context
                  .read<RouteAssignmentBloc>()
                  .add(const LoadRepsRequested()),
              style: OutlinedButton.styleFrom(
                foregroundColor: AppColors.primary,
                side: BorderSide(color: AppColors.primary),
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(10.r)),
              ),
              child: Text('Retry',
                  style: GoogleFonts.barlowCondensed(
                      fontSize: 15.sp, fontWeight: FontWeight.w700)),
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
      padding: EdgeInsets.fromLTRB(16.w, 8.h, 16.w, 32.h),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // ── Rep selector ────────────────────────────────────────────────
          _FieldLabel('SALES REP'),
          SizedBox(height: 6.h),
          _DropdownField<RepSummary>(
            value: selectedRep,
            items: reps,
            labelBuilder: (r) => r.userName,
            onChanged: isSaving
                ? null
                : (rep) {
                    if (rep != null) {
                      context
                          .read<RouteAssignmentBloc>()
                          .add(RepSelected(rep));
                    }
                  },
          ),
          SizedBox(height: 20.h),

          // ── Route selector ──────────────────────────────────────────────
          _FieldLabel('ROUTE'),
          SizedBox(height: 6.h),
          if (isLoadingRoutes)
            _RouteLoadingIndicator()
          else if (routes.isEmpty)
            _NoRoutesWarning()
          else
            _DropdownField<RepRoute>(
              value: selectedRoute,
              items: routes,
              labelBuilder: (r) => r.routeName,
              onChanged: isSaving
                  ? null
                  : (route) {
                      if (route != null) {
                        context
                            .read<RouteAssignmentBloc>()
                            .add(RouteSelected(route));
                      }
                    },
            ),
          SizedBox(height: 20.h),

          // ── Date picker ─────────────────────────────────────────────────
          _FieldLabel('DATE'),
          SizedBox(height: 6.h),
          _DatePickerField(
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
          SizedBox(height: 24.h),

          // ── Error banner ────────────────────────────────────────────────
          if (errorMessage != null) ...[
            _ErrorBanner(message: errorMessage!),
            SizedBox(height: 16.h),
          ],

          // ── Submit button ───────────────────────────────────────────────
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

// ── Field label ───────────────────────────────────────────────────────────────
class _FieldLabel extends StatelessWidget {
  final String text;
  const _FieldLabel(this.text);

  @override
  Widget build(BuildContext context) {
    return Text(
      text,
      style: GoogleFonts.barlowCondensed(
        fontSize: 11.sp,
        fontWeight: FontWeight.w700,
        letterSpacing: 2.0,
        color: AppColors.foregroundMuted,
      ),
    );
  }
}

// ── Generic dropdown ──────────────────────────────────────────────────────────
class _DropdownField<T> extends StatelessWidget {
  final T? value;
  final List<T> items;
  final String Function(T) labelBuilder;
  final ValueChanged<T?>? onChanged;

  const _DropdownField({
    required this.value,
    required this.items,
    required this.labelBuilder,
    required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.symmetric(horizontal: 14.w),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: AppColors.surfaceVariant),
      ),
      child: DropdownButtonHideUnderline(
        child: DropdownButton<T>(
          value: value,
          isExpanded: true,
          icon: Icon(Icons.keyboard_arrow_down_rounded,
              color: AppColors.foregroundMuted, size: 20.r),
          style: GoogleFonts.barlow(
            fontSize: 15.sp,
            color: AppColors.foreground,
          ),
          items: items
              .map((item) => DropdownMenuItem<T>(
                    value: item,
                    child: Text(labelBuilder(item)),
                  ))
              .toList(),
          onChanged: onChanged,
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
      height: 52.h,
      padding: EdgeInsets.symmetric(horizontal: 14.w),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: AppColors.surfaceVariant),
      ),
      child: Row(
        children: [
          SizedBox(
            width: 16.r,
            height: 16.r,
            child: CircularProgressIndicator(
              strokeWidth: 2,
              color: AppColors.primary,
            ),
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
      padding: EdgeInsets.all(14.r),
      decoration: BoxDecoration(
        color: AppColors.warning.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: AppColors.warning.withValues(alpha: 0.3)),
      ),
      child: Row(
        children: [
          Icon(Icons.warning_amber_rounded,
              color: AppColors.warning, size: 18.r),
          SizedBox(width: 10.w),
          Expanded(
            child: Text(
              'This rep has no routes assigned. Ask an admin to configure their geo assignment first.',
              style: GoogleFonts.barlow(
                  fontSize: 13.sp, color: AppColors.foreground, height: 1.4),
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
    return '${dt.day} ${months[dt.month - 1]} ${dt.year}';
  }

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: enabled ? onTap : null,
      child: Container(
        height: 52.h,
        padding: EdgeInsets.symmetric(horizontal: 14.w),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12.r),
          border: Border.all(color: AppColors.surfaceVariant),
        ),
        child: Row(
          children: [
            Icon(Icons.calendar_today_rounded,
                size: 16.r, color: AppColors.primary),
            SizedBox(width: 10.w),
            Expanded(
              child: Text(
                selectedDate != null ? _format(selectedDate!) : 'Select a date',
                style: GoogleFonts.barlow(
                  fontSize: 15.sp,
                  color: selectedDate != null
                      ? AppColors.foreground
                      : AppColors.foregroundMuted,
                ),
              ),
            ),
            Icon(Icons.keyboard_arrow_down_rounded,
                color: AppColors.foregroundMuted, size: 20.r),
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
        color: AppColors.error.withValues(alpha: 0.07),
        borderRadius: BorderRadius.circular(12.r),
        border: Border.all(color: AppColors.error.withValues(alpha: 0.3)),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(Icons.error_outline_rounded,
              color: AppColors.error, size: 18.r),
          SizedBox(width: 10.w),
          Expanded(
            child: Text(
              message,
              style: GoogleFonts.barlow(
                  fontSize: 13.sp, color: AppColors.foreground, height: 1.4),
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
    return SizedBox(
      width: double.infinity,
      height: 54.h,
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: canSubmit ? onTap : null,
          borderRadius: BorderRadius.circular(12.r),
          child: Ink(
            decoration: BoxDecoration(
              color: canSubmit
                  ? AppColors.primary
                  : AppColors.primary.withValues(alpha: 0.35),
              borderRadius: BorderRadius.circular(12.r),
              boxShadow: canSubmit
                  ? [
                      BoxShadow(
                        color: AppColors.primary.withValues(alpha: 0.30),
                        blurRadius: 14,
                        offset: const Offset(0, 5),
                      ),
                    ]
                  : null,
            ),
            child: Center(
              child: isSaving
                  ? SizedBox(
                      width: 20.r,
                      height: 20.r,
                      child: const CircularProgressIndicator(
                        color: Colors.white,
                        strokeWidth: 2.5,
                      ),
                    )
                  : Text(
                      'ASSIGN ROUTE',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 16.sp,
                        fontWeight: FontWeight.w800,
                        letterSpacing: 1.5,
                        color: Colors.white,
                      ),
                    ),
            ),
          ),
        ),
      ),
    );
  }
}
