import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:flutter_spinkit/flutter_spinkit.dart';
import 'package:uswatte/core/theme/app_theme.dart';

enum _SpinnerVariant { page, button, small }

/// Unified loading indicator used throughout the app.
///
///   AppSpinner()        → SpinKitRing, 30.r  (page / section loading)
///   AppSpinner.button() → CircularProgressIndicator, 18.r, white (inside buttons)
///   AppSpinner.small()  → CircularProgressIndicator, 18.r  (header icons, pagination)
class AppSpinner extends StatelessWidget {
  final _SpinnerVariant _variant;
  final double? _sizeOverride;
  final Color? _colorOverride;

  const AppSpinner({
    super.key,
    double? size,
    Color? color,
  })  : _variant = _SpinnerVariant.page,
        _sizeOverride = size,
        _colorOverride = color;

  const AppSpinner.button({super.key, Color? color})
      : _variant = _SpinnerVariant.button,
        _sizeOverride = null,
        _colorOverride = color;

  const AppSpinner.small({super.key, Color? color})
      : _variant = _SpinnerVariant.small,
        _sizeOverride = null,
        _colorOverride = color;

  @override
  Widget build(BuildContext context) {
    switch (_variant) {
      case _SpinnerVariant.page:
        return SpinKitRing(
          color: _colorOverride ?? AppColors.primary,
          size: _sizeOverride ?? 30.r,
          lineWidth: 3.0,
        );

      case _SpinnerVariant.button:
        final double size = _sizeOverride ?? 18.r;
        return SizedBox(
          width: size,
          height: size,
          child: CircularProgressIndicator(
            strokeWidth: 2.0,
            color: _colorOverride ?? Colors.white,
          ),
        );

      case _SpinnerVariant.small:
        final double size = _sizeOverride ?? 18.r;
        return SizedBox(
          width: size,
          height: size,
          child: CircularProgressIndicator(
            strokeWidth: 2.0,
            color: _colorOverride ?? AppColors.primary,
          ),
        );
    }
  }
}
