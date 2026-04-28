import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';

/// Shown during [AuthInitial] — the brief window while the app checks
/// secure storage for a saved token. Prevents the login-page flash
/// on cold start when the user already has a valid session.
class SplashPage extends StatelessWidget {
  const SplashPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: Center(
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Image.asset(
              'assets/images/uswatte-logo.png',
              height: 80.h,
              fit: BoxFit.contain,
            ),
            SizedBox(height: 8.h),
            Text(
              'SALES FORCE APPLICATION',
              style: GoogleFonts.barlowCondensed(
                fontSize: 11.sp,
                fontWeight: FontWeight.w700,
                letterSpacing: 3.0,
                color: AppColors.foregroundMuted,
              ),
            ),
            SizedBox(height: 48.h),
            SizedBox(
              width: 20.r,
              height: 20.r,
              child: CircularProgressIndicator(
                strokeWidth: 2,
                color: AppColors.primary,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
