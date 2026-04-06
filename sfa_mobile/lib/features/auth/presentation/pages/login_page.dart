import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/auth/presentation/bloc/auth_bloc.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage>
    with SingleTickerProviderStateMixin {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;
  late AnimationController _animController;
  late Animation<double> _fadeAnim;
  late Animation<Offset> _slideAnim;

  @override
  void initState() {
    super.initState();
    _animController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 900),
    );
    _fadeAnim = CurvedAnimation(
      parent: _animController,
      curve: const Interval(0.0, 0.7, curve: Curves.easeOut),
    );
    _slideAnim = Tween<Offset>(
      begin: const Offset(0, 0.06),
      end: Offset.zero,
    ).animate(CurvedAnimation(
      parent: _animController,
      curve: const Interval(0.1, 1.0, curve: Curves.easeOutCubic),
    ));
    _animController.forward();
  }

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    _animController.dispose();
    super.dispose();
  }

  void _submit() {
    if (_formKey.currentState?.validate() ?? false) {
      context.read<AuthBloc>().add(
            LoginSubmitted(
              username: _usernameController.text.trim(),
              password: _passwordController.text,
            ),
          );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      body: BlocListener<AuthBloc, AuthState>(
        listener: (context, state) {
          if (state is AuthFailure) {
            ScaffoldMessenger.of(context).showSnackBar(
              SnackBar(
                content: Text(
                  state.message,
                  style: GoogleFonts.barlow(
                    fontWeight: FontWeight.w500,
                    fontSize: 13.sp,
                    color: Colors.white,
                  ),
                ),
                backgroundColor: AppColors.error,
                behavior: SnackBarBehavior.floating,
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(6.r)),
                margin: EdgeInsets.all(16.w),
              ),
            );
          }
        },
        child: Column(
          children: [
            _HeaderBlock(),
            Expanded(
              child: SafeArea(
                top: false,
                child: SingleChildScrollView(
                  padding: EdgeInsets.symmetric(
                      horizontal: 28.w, vertical: 32.h),
                  child: FadeTransition(
                    opacity: _fadeAnim,
                    child: SlideTransition(
                      position: _slideAnim,
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            'SIGN IN',
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 12.sp,
                              fontWeight: FontWeight.w700,
                              letterSpacing: 3.0,
                              color: AppColors.primary,
                            ),
                          ),
                          SizedBox(height: 6.h),
                          Text(
                            'Welcome back',
                            style: GoogleFonts.barlowCondensed(
                              fontSize: 22.sp,
                              fontWeight: FontWeight.w700,
                              color: AppColors.foreground,
                            ),
                          ),
                          SizedBox(height: 28.h),

                          Form(
                            key: _formKey,
                            child: Column(
                              children: [
                                _FormField(
                                  controller: _usernameController,
                                  label: 'USERNAME',
                                  hint: 'Enter your username',
                                  icon: Icons.person_outline_rounded,
                                  validator: (v) =>
                                      (v == null || v.trim().isEmpty)
                                          ? 'Username is required'
                                          : null,
                                ),
                                SizedBox(height: 16.h),
                                _FormField(
                                  controller: _passwordController,
                                  label: 'PASSWORD',
                                  hint: 'Enter your password',
                                  icon: Icons.lock_outline_rounded,
                                  obscureText: _obscurePassword,
                                  suffixIcon: IconButton(
                                    icon: Icon(
                                      _obscurePassword
                                          ? Icons.visibility_off_outlined
                                          : Icons.visibility_outlined,
                                      size: 20.r,
                                      color: AppColors.foregroundMuted,
                                    ),
                                    onPressed: () => setState(() =>
                                        _obscurePassword = !_obscurePassword),
                                  ),
                                  validator: (v) =>
                                      (v == null || v.isEmpty)
                                          ? 'Password is required'
                                          : null,
                                ),
                                SizedBox(height: 28.h),

                                // Submit button
                                BlocBuilder<AuthBloc, AuthState>(
                                  builder: (context, state) {
                                    final isLoading = state is AuthLoading;
                                    return _GradientButton(
                                      onPressed: isLoading ? null : _submit,
                                      isLoading: isLoading,
                                    );
                                  },
                                ),
                              ],
                            ),
                          ),

                          SizedBox(height: 36.h),

                          Center(
                            child: Text(
                              'SFA Uswatte  ·  Field Sales Platform',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 11.sp,
                                fontWeight: FontWeight.w500,
                                letterSpacing: 1.2,
                                color: AppColors.foregroundMuted,
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Header ────────────────────────────────────────────────────────────────────
class _HeaderBlock extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      color: Colors.white,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(height: 5.h, color: AppColors.primary),
          SafeArea(
            bottom: false,
            child: Padding(
              padding: EdgeInsets.fromLTRB(28.w, 28.h, 28.w, 32.h),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Top nav
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.center,
                    children: [
                      Text(
                        'SFA',
                        style: GoogleFonts.barlowCondensed(
                          fontSize: 12.sp,
                          fontWeight: FontWeight.w700,
                          letterSpacing: 4.0,
                          color: AppColors.foregroundMuted,
                        ),
                      ),
                      const Spacer(),
                      Container(
                        padding: EdgeInsets.symmetric(
                            horizontal: 8.w, vertical: 3.h),
                        decoration: BoxDecoration(
                          border:
                              Border.all(color: AppColors.surfaceVariant),
                          borderRadius: BorderRadius.circular(3.r),
                        ),
                        child: Text(
                          'v1.0',
                          style: GoogleFonts.barlowCondensed(
                            fontSize: 10.sp,
                            fontWeight: FontWeight.w600,
                            letterSpacing: 1.0,
                            color: AppColors.foregroundMuted,
                          ),
                        ),
                      ),
                    ],
                  ),

                  SizedBox(height: 24.h),

                  // Logo + descriptor
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.center,
                    children: [
                      Image.asset(
                        'assets/images/uswatte-logo.png',
                        height: 100.h,
                        fit: BoxFit.contain,
                      ),
                      SizedBox(width: 20.w),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              'FIELD SALES\nPLATFORM',
                              style: GoogleFonts.barlowCondensed(
                                fontSize: 28.sp,
                                fontWeight: FontWeight.w800,
                                letterSpacing: -0.5,
                                height: 0.95,
                                color: AppColors.foreground,
                              ),
                            ),
                            SizedBox(height: 10.h),
                            Row(
                              children: [
                                Container(
                                    height: 3.h,
                                    width: 24.w,
                                    color: AppColors.primary),
                                SizedBox(width: 5.w),
                                Container(
                                    height: 3.h,
                                    width: 8.w,
                                    color: AppColors.amber),
                              ],
                            ),
                          ],
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
}

// ── Gradient button ───────────────────────────────────────────────────────────
class _GradientButton extends StatelessWidget {
  const _GradientButton({required this.onPressed, required this.isLoading});
  final VoidCallback? onPressed;
  final bool isLoading;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      height: 52.h,
      child: DecoratedBox(
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(10.r),
          gradient: onPressed == null
              ? LinearGradient(colors: [
                  AppColors.primaryLight.withValues(alpha: 0.5),
                  AppColors.amber.withValues(alpha: 0.5),
                ])
              : LinearGradient(
                  begin: Alignment.centerLeft,
                  end: Alignment.centerRight,
                  colors: [AppColors.primary, AppColors.primaryLight],
                ),
          boxShadow: onPressed == null
              ? []
              : [
                  BoxShadow(
                    color: AppColors.primary.withValues(alpha: 0.35),
                    blurRadius: 16,
                    offset: const Offset(0, 6),
                  ),
                ],
        ),
        child: MaterialButton(
          onPressed: onPressed,
          shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(10.r)),
          padding: EdgeInsets.zero,
          child: isLoading
              ? SizedBox(
                  height: 22.r,
                  width: 22.r,
                  child: const CircularProgressIndicator(
                    strokeWidth: 2.5,
                    color: Colors.white,
                  ),
                )
              : Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Text(
                      'SIGN IN',
                      style: GoogleFonts.barlowCondensed(
                        fontSize: 16.sp,
                        fontWeight: FontWeight.w700,
                        letterSpacing: 3.0,
                        color: Colors.white,
                      ),
                    ),
                    SizedBox(width: 10.w),
                    Icon(Icons.arrow_forward_rounded,
                        color: Colors.white, size: 18.r),
                  ],
                ),
        ),
      ),
    );
  }
}

// ── Form field ────────────────────────────────────────────────────────────────
class _FormField extends StatelessWidget {
  const _FormField({
    required this.controller,
    required this.label,
    required this.hint,
    required this.icon,
    this.obscureText = false,
    this.suffixIcon,
    this.validator,
  });

  final TextEditingController controller;
  final String label;
  final String hint;
  final IconData icon;
  final bool obscureText;
  final Widget? suffixIcon;
  final String? Function(String?)? validator;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: GoogleFonts.barlowCondensed(
            fontSize: 11.sp,
            fontWeight: FontWeight.w700,
            letterSpacing: 2.0,
            color: AppColors.foregroundMuted,
          ),
        ),
        SizedBox(height: 6.h),
        TextFormField(
          controller: controller,
          obscureText: obscureText,
          style: GoogleFonts.barlow(
            fontSize: 15.sp,
            fontWeight: FontWeight.w500,
            color: AppColors.foreground,
          ),
          decoration: InputDecoration(
            hintText: hint,
            prefixIcon: Icon(icon, size: 18.r),
            suffixIcon: suffixIcon,
          ),
          validator: validator,
        ),
      ],
    );
  }
}
