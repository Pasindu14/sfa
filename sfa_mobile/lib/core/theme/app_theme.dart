import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:google_fonts/google_fonts.dart';

class AppColors {
  AppColors._();

  // ── Primary amber-orange scale (mirrors sfa_web oklch chart palette) ──
  static const Color primary = Color(0xFFCA5D10);       // web: --primary oklch(0.553 0.195 38.4)
  static const Color primaryDark = Color(0xFF9A3D08);   // web: --chart-5 oklch(0.47 0.157 37.3)
  static const Color primaryMedium = Color(0xFFBD6318);  // web: sidebar-primary oklch(0.646 0.222 41.1)
  static const Color primaryLight = Color(0xFFDA7E22);   // web: --chart-2 oklch(0.705 0.213 47.6)
  static const Color amber = Color(0xFFEEAA52);          // web: --chart-1 oklch(0.837 0.128 66.29)

  // ── Warm neutral scale ──
  static const Color background = Color(0xFFFFFFFF);     // web: --background
  static const Color surface = Color(0xFFF6F5EF);        // web: --muted oklch(0.966 0.005 106.5)
  static const Color surfaceVariant = Color(0xFFEEEDE6); // web: --border oklch(0.93 0.007 106.5)
  static const Color foreground = Color(0xFF1A1A11);     // web: --foreground oklch(0.153 0.006 107.1)
  static const Color foregroundMuted = Color(0xFF7A7260);// web: --muted-foreground oklch(0.58 0.031 107.3)
  static const Color onPrimary = Color(0xFFF9F2E4);      // web: --primary-foreground oklch(0.98 0.016 73.684)

  // ── Functional ──
  static const Color error = Color(0xFFCC4218);          // web: --destructive oklch(0.577 0.245 27.325)
  static const Color success = Color(0xFF2E7D52);
  static const Color warning = Color(0xFFB45309);

  // ── Dark surface (appbar, nav) ──
  static const Color darkSurface = Color(0xFF1A1A11);    // same as foreground
  static const Color darkSurfaceCard = Color(0xFF2A2920);
}

class AppTheme {
  AppTheme._();

  // ── Typography ──
  static TextTheme _buildTextTheme() {
    final condensed = GoogleFonts.barlowCondensedTextTheme().copyWith(
      displayLarge: GoogleFonts.barlowCondensed(
        fontSize: 57,
        fontWeight: FontWeight.w700,
        letterSpacing: -1.5,
        color: AppColors.foreground,
      ),
      displayMedium: GoogleFonts.barlowCondensed(
        fontSize: 45,
        fontWeight: FontWeight.w700,
        letterSpacing: -1,
        color: AppColors.foreground,
      ),
      displaySmall: GoogleFonts.barlowCondensed(
        fontSize: 36,
        fontWeight: FontWeight.w700,
        letterSpacing: -0.5,
        color: AppColors.foreground,
      ),
      headlineLarge: GoogleFonts.barlowCondensed(
        fontSize: 32,
        fontWeight: FontWeight.w700,
        letterSpacing: 0,
        color: AppColors.foreground,
      ),
      headlineMedium: GoogleFonts.barlowCondensed(
        fontSize: 28,
        fontWeight: FontWeight.w700,
        letterSpacing: 0.2,
        color: AppColors.foreground,
      ),
      headlineSmall: GoogleFonts.barlowCondensed(
        fontSize: 24,
        fontWeight: FontWeight.w600,
        letterSpacing: 0.3,
        color: AppColors.foreground,
      ),
      titleLarge: GoogleFonts.barlowCondensed(
        fontSize: 22,
        fontWeight: FontWeight.w600,
        letterSpacing: 0.3,
        color: AppColors.foreground,
      ),
      titleMedium: GoogleFonts.barlowCondensed(
        fontSize: 16,
        fontWeight: FontWeight.w600,
        letterSpacing: 0.8,
        color: AppColors.foreground,
      ),
      titleSmall: GoogleFonts.barlowCondensed(
        fontSize: 14,
        fontWeight: FontWeight.w600,
        letterSpacing: 0.8,
        color: AppColors.foreground,
      ),
      bodyLarge: GoogleFonts.barlow(
        fontSize: 16,
        fontWeight: FontWeight.w400,
        letterSpacing: 0.1,
        color: AppColors.foreground,
      ),
      bodyMedium: GoogleFonts.barlow(
        fontSize: 14,
        fontWeight: FontWeight.w400,
        letterSpacing: 0.15,
        color: AppColors.foreground,
      ),
      bodySmall: GoogleFonts.barlow(
        fontSize: 12,
        fontWeight: FontWeight.w400,
        letterSpacing: 0.2,
        color: AppColors.foregroundMuted,
      ),
      labelLarge: GoogleFonts.barlowCondensed(
        fontSize: 14,
        fontWeight: FontWeight.w600,
        letterSpacing: 1.2,
        color: AppColors.foreground,
      ),
      labelMedium: GoogleFonts.barlowCondensed(
        fontSize: 12,
        fontWeight: FontWeight.w600,
        letterSpacing: 1.2,
        color: AppColors.foregroundMuted,
      ),
      labelSmall: GoogleFonts.barlowCondensed(
        fontSize: 11,
        fontWeight: FontWeight.w600,
        letterSpacing: 1.5,
        color: AppColors.foregroundMuted,
      ),
    );
    return condensed;
  }

  static ThemeData get light {
    final colorScheme = ColorScheme(
      brightness: Brightness.light,
      primary: AppColors.primary,
      onPrimary: AppColors.onPrimary,
      primaryContainer: AppColors.amber,
      onPrimaryContainer: AppColors.primaryDark,
      secondary: AppColors.primaryMedium,
      onSecondary: AppColors.onPrimary,
      secondaryContainer: AppColors.surface,
      onSecondaryContainer: AppColors.foreground,
      tertiary: AppColors.amber,
      onTertiary: AppColors.primaryDark,
      error: AppColors.error,
      onError: AppColors.onPrimary,
      surface: AppColors.background,
      onSurface: AppColors.foreground,
      surfaceContainerHighest: AppColors.surfaceVariant,
      outline: AppColors.surfaceVariant,
      outlineVariant: AppColors.surfaceVariant,
    );

    final textTheme = _buildTextTheme();

    return ThemeData(
      useMaterial3: true,
      colorScheme: colorScheme,
      textTheme: textTheme,
      scaffoldBackgroundColor: AppColors.background,

      // ── AppBar ──
      appBarTheme: AppBarTheme(
        backgroundColor: AppColors.darkSurface,
        foregroundColor: AppColors.onPrimary,
        elevation: 0,
        centerTitle: false,
        systemOverlayStyle: const SystemUiOverlayStyle(
          statusBarColor: Colors.transparent,
          statusBarIconBrightness: Brightness.light,
        ),
        titleTextStyle: GoogleFonts.barlowCondensed(
          fontSize: 20,
          fontWeight: FontWeight.w700,
          letterSpacing: 0.5,
          color: AppColors.onPrimary,
        ),
        iconTheme: const IconThemeData(color: AppColors.onPrimary),
        actionsIconTheme: const IconThemeData(color: AppColors.onPrimary),
      ),

      // ── Elevated Button ──
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.primary,
          foregroundColor: AppColors.onPrimary,
          minimumSize: const Size(double.infinity, 52),
          elevation: 0,
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
          textStyle: GoogleFonts.barlowCondensed(
            fontSize: 16,
            fontWeight: FontWeight.w700,
            letterSpacing: 2.0,
          ),
        ),
      ),

      // ── Text Button ──
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: AppColors.primary,
          textStyle: GoogleFonts.barlowCondensed(
            fontSize: 14,
            fontWeight: FontWeight.w600,
            letterSpacing: 0.8,
          ),
        ),
      ),

      // ── Input Decoration ──
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: AppColors.surface,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 16),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(4),
          borderSide: const BorderSide(color: AppColors.surfaceVariant, width: 1.5),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(4),
          borderSide: const BorderSide(color: AppColors.surfaceVariant, width: 1.5),
        ),
        focusedBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(4),
          borderSide: const BorderSide(color: AppColors.primary, width: 2),
        ),
        errorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(4),
          borderSide: const BorderSide(color: AppColors.error, width: 1.5),
        ),
        focusedErrorBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(4),
          borderSide: const BorderSide(color: AppColors.error, width: 2),
        ),
        labelStyle: GoogleFonts.barlowCondensed(
          fontSize: 14,
          fontWeight: FontWeight.w500,
          letterSpacing: 0.5,
          color: AppColors.foregroundMuted,
        ),
        hintStyle: GoogleFonts.barlow(
          fontSize: 14,
          fontWeight: FontWeight.w400,
          color: AppColors.foregroundMuted,
        ),
        prefixIconColor: AppColors.foregroundMuted,
        errorStyle: GoogleFonts.barlow(
          fontSize: 12,
          color: AppColors.error,
        ),
      ),

      // ── Card ──
      cardTheme: CardThemeData(
        color: AppColors.background,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(6),
          side: const BorderSide(color: AppColors.surfaceVariant, width: 1),
        ),
        margin: EdgeInsets.zero,
      ),

      // ── Chip ──
      chipTheme: ChipThemeData(
        backgroundColor: AppColors.surface,
        labelStyle: GoogleFonts.barlowCondensed(
          fontSize: 12,
          fontWeight: FontWeight.w600,
          letterSpacing: 0.5,
          color: AppColors.foreground,
        ),
        side: const BorderSide(color: AppColors.surfaceVariant),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(4)),
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 0),
      ),

      // ── Divider ──
      dividerTheme: const DividerThemeData(
        color: AppColors.surfaceVariant,
        thickness: 1,
        space: 1,
      ),

      // ── Bottom Navigation ──
      bottomNavigationBarTheme: BottomNavigationBarThemeData(
        backgroundColor: AppColors.darkSurface,
        selectedItemColor: AppColors.amber,
        unselectedItemColor: AppColors.onPrimary.withValues(alpha: 0.45),
        selectedLabelStyle: GoogleFonts.barlowCondensed(
          fontSize: 11,
          fontWeight: FontWeight.w700,
          letterSpacing: 0.8,
        ),
        unselectedLabelStyle: GoogleFonts.barlowCondensed(
          fontSize: 11,
          fontWeight: FontWeight.w500,
          letterSpacing: 0.5,
        ),
        type: BottomNavigationBarType.fixed,
        elevation: 0,
      ),
    );
  }
}
