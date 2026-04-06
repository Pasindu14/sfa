import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';

/// Shown when a user with a non-SalesRep role logs in.
/// The mobile app is exclusively for field sales representatives.
class UnsupportedRolePage extends StatelessWidget {
  const UnsupportedRolePage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: SafeArea(
        child: Padding(
          padding: EdgeInsets.symmetric(horizontal: 32.w),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                width: 72.r,
                height: 72.r,
                decoration: BoxDecoration(
                  color: AppColors.primary.withValues(alpha: 0.08),
                  shape: BoxShape.circle,
                ),
                child: Icon(
                  Icons.phone_android_rounded,
                  size: 32.r,
                  color: AppColors.primary,
                ),
              ),
              SizedBox(height: 24.h),
              Text(
                'App Not Available\nFor Your Role',
                textAlign: TextAlign.center,
                style: GoogleFonts.barlowCondensed(
                  fontSize: 26.sp,
                  fontWeight: FontWeight.w800,
                  height: 1.1,
                  color: AppColors.foreground,
                ),
              ),
              SizedBox(height: 12.h),
              Text(
                'This mobile app is designed exclusively for\nfield sales representatives. Please use the\nweb portal to access your account.',
                textAlign: TextAlign.center,
                style: GoogleFonts.barlow(
                  fontSize: 14.sp,
                  height: 1.5,
                  color: AppColors.foregroundMuted,
                ),
              ),
              SizedBox(height: 36.h),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: () =>
                      context.read<AuthBloc>().add(const LogoutRequested()),
                  style: ElevatedButton.styleFrom(
                    backgroundColor: AppColors.primary,
                    foregroundColor: Colors.white,
                    padding: EdgeInsets.symmetric(vertical: 16.h),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12.r),
                    ),
                    elevation: 0,
                  ),
                  child: Text(
                    'Sign Out',
                    style: GoogleFonts.barlowCondensed(
                      fontSize: 16.sp,
                      fontWeight: FontWeight.w700,
                      letterSpacing: 1.0,
                    ),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
