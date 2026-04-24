import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:uswatte/core/constants/sl_geo.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/create_outlet/presentation/bloc/create_outlet_bloc.dart';
import 'package:uswatte/features/create_outlet/presentation/bloc/create_outlet_event.dart';
import 'package:uswatte/features/create_outlet/presentation/bloc/create_outlet_state.dart';

class CreateOutletPage extends StatefulWidget {
  const CreateOutletPage({super.key});

  @override
  State<CreateOutletPage> createState() => _CreateOutletPageState();
}

class _CreateOutletPageState extends State<CreateOutletPage> {
  final _nameCtr = TextEditingController();
  final _addressCtr = TextEditingController();
  final _telCtr = TextEditingController();
  final _nicCtr = TextEditingController();
  final _emailCtr = TextEditingController();
  final _contactCtr = TextEditingController();
  final _vatCtr = TextEditingController();
  final _creditCtr = TextEditingController(text: '0');
  final _remarksCtr = TextEditingController();
  final _dobCtr = TextEditingController();
  final _imageCtr = TextEditingController();
  final _latCtr = TextEditingController();
  final _lngCtr = TextEditingController();

  @override
  void dispose() {
    for (final c in [
      _nameCtr, _addressCtr, _telCtr, _nicCtr, _emailCtr, _contactCtr,
      _vatCtr, _creditCtr, _remarksCtr, _dobCtr, _imageCtr, _latCtr, _lngCtr,
    ]) {
      c.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    SystemChrome.setSystemUIOverlayStyle(const SystemUiOverlayStyle(
      statusBarColor: Colors.transparent,
      statusBarIconBrightness: Brightness.light,
    ));

    return BlocConsumer<CreateOutletBloc, CreateOutletState>(
      listenWhen: (prev, curr) =>
          prev.submitted != curr.submitted ||
          prev.submitError != curr.submitError,
      listener: (context, state) {
        if (state.submitted) {
          ScaffoldMessenger.of(context).showSnackBar(SnackBar(
            content: Text(
              'Outlet registered successfully.',
              style: GoogleFonts.barlow(color: Colors.white),
            ),
            backgroundColor: AppColors.success,
            duration: const Duration(seconds: 3),
          ));
          context.pop();
        }
        if (state.submitError != null) {
          ScaffoldMessenger.of(context).showSnackBar(SnackBar(
            content: Text(
              state.submitError!,
              style: GoogleFonts.barlow(color: Colors.white),
            ),
            backgroundColor: AppColors.error,
            duration: const Duration(seconds: 4),
          ));
        }
      },
      builder: (context, state) {
        return Scaffold(
          backgroundColor: AppColors.background,
          body: Column(
            children: [
              _AppBar(onBack: () => context.pop()),
              Expanded(
                child: CustomScrollView(
                  slivers: [
                    SliverToBoxAdapter(child: SizedBox(height: 8.h)),
                    _SectionHeader(number: '1', label: 'OUTLET INFO'),
                    SliverToBoxAdapter(
                      child: Padding(
                        padding: EdgeInsets.symmetric(horizontal: 16.w),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            SizedBox(height: 12.h),
                            _Field(
                              label: 'Outlet Name *',
                              controller: _nameCtr,
                              error: state.errors['name'],
                              onChanged: (v) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletNameChanged(v)),
                            ),
                            SizedBox(height: 12.h),
                            _Field(
                              label: 'Address *',
                              controller: _addressCtr,
                              maxLines: 3,
                              error: state.errors['address'],
                              onChanged: (v) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletAddressChanged(v)),
                            ),
                            SizedBox(height: 12.h),
                            _Field(
                              label: 'Phone *',
                              controller: _telCtr,
                              keyboardType: TextInputType.phone,
                              error: state.errors['tel'],
                              onChanged: (v) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletTelChanged(v)),
                            ),
                            SizedBox(height: 12.h),
                            _Field(
                              label: 'NIC No. *',
                              controller: _nicCtr,
                              error: state.errors['nicNo'],
                              onChanged: (v) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletNicNoChanged(v)),
                            ),
                            SizedBox(height: 16.h),
                            _SegmentLabel('Outlet Type *'),
                            SizedBox(height: 8.h),
                            _SegmentedSelector(
                              options: const ['Small', 'Medium', 'Large'],
                              selected: state.outletType,
                              error: state.errors['outletType'],
                              onSelect: (v) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletTypeChanged(v)),
                            ),
                            SizedBox(height: 16.h),
                            _SegmentLabel('Outlet Category *'),
                            SizedBox(height: 8.h),
                            _SegmentedSelector(
                              options: const ['Wholesale', 'SMMT'],
                              selected: state.outletCategory,
                              error: state.errors['outletCategory'],
                              onSelect: (v) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletCategoryChanged(v)),
                            ),
                            SizedBox(height: 8.h),
                          ],
                        ),
                      ),
                    ),
                    _SectionHeader(number: '2', label: 'LOCATION'),
                    SliverToBoxAdapter(
                      child: Padding(
                        padding: EdgeInsets.symmetric(horizontal: 16.w),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            SizedBox(height: 12.h),
                            Row(
                              children: [
                                Expanded(
                                  child: _Field(
                                    label: 'Latitude',
                                    controller: _latCtr
                                      ..text = state.latitude != null
                                          ? state.latitude!
                                              .toStringAsFixed(6)
                                          : '',
                                    keyboardType:
                                        const TextInputType.numberWithOptions(
                                            decimal: true, signed: true),
                                    error: state.errors['location'],
                                    onChanged: (v) {
                                      final lat = double.tryParse(v);
                                      if (lat != null) {
                                        context
                                            .read<CreateOutletBloc>()
                                            .add(OutletLatLngManualChanged(
                                                lat: lat,
                                                lng: state.longitude));
                                      }
                                    },
                                  ),
                                ),
                                SizedBox(width: 12.w),
                                Expanded(
                                  child: _Field(
                                    label: 'Longitude',
                                    controller: _lngCtr
                                      ..text = state.longitude != null
                                          ? state.longitude!
                                              .toStringAsFixed(6)
                                          : '',
                                    keyboardType:
                                        const TextInputType.numberWithOptions(
                                            decimal: true, signed: true),
                                    onChanged: (v) {
                                      final lng = double.tryParse(v);
                                      if (lng != null) {
                                        context
                                            .read<CreateOutletBloc>()
                                            .add(OutletLatLngManualChanged(
                                                lat: state.latitude,
                                                lng: lng));
                                      }
                                    },
                                  ),
                                ),
                              ],
                            ),
                            if (state.errors['location'] != null) ...[
                              SizedBox(height: 4.h),
                              Text(
                                state.errors['location']!,
                                style: GoogleFonts.barlow(
                                    fontSize: 11.sp, color: AppColors.error),
                              ),
                            ],
                            SizedBox(height: 12.h),
                            SizedBox(
                              width: double.infinity,
                              child: OutlinedButton.icon(
                                onPressed: state.isLocating
                                    ? null
                                    : () => context
                                        .read<CreateOutletBloc>()
                                        .add(const OutletLocationCaptureRequested()),
                                style: OutlinedButton.styleFrom(
                                  foregroundColor: AppColors.primary,
                                  side: BorderSide(
                                      color: AppColors.primary
                                          .withValues(alpha: 0.5)),
                                  padding:
                                      EdgeInsets.symmetric(vertical: 14.h),
                                  shape: RoundedRectangleBorder(
                                      borderRadius:
                                          BorderRadius.circular(10.r)),
                                ),
                                icon: state.isLocating
                                    ? SizedBox(
                                        width: 16.r,
                                        height: 16.r,
                                        child: CircularProgressIndicator(
                                            strokeWidth: 2,
                                            color: AppColors.primary),
                                      )
                                    : Icon(Icons.my_location_rounded,
                                        size: 16.r),
                                label: Text(
                                  state.isLocating
                                      ? 'Getting Location…'
                                      : state.hasLocation
                                          ? 'Location Captured  ✓'
                                          : 'USE MY LOCATION',
                                  style: GoogleFonts.barlowCondensed(
                                    fontSize: 13.sp,
                                    fontWeight: FontWeight.w700,
                                    letterSpacing: 1.0,
                                  ),
                                ),
                              ),
                            ),
                            SizedBox(height: 8.h),
                          ],
                        ),
                      ),
                    ),
                    _SectionHeader(number: '3', label: 'OPTIONAL DETAILS'),
                    SliverToBoxAdapter(
                      child: Padding(
                        padding: EdgeInsets.symmetric(horizontal: 16.w),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            SizedBox(height: 12.h),
                            _Field(
                              label: 'Contact Person',
                              controller: _contactCtr,
                              onChanged: (v) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletContactPersonChanged(v)),
                            ),
                            SizedBox(height: 12.h),
                            _Field(
                              label: 'Email',
                              controller: _emailCtr,
                              keyboardType: TextInputType.emailAddress,
                              onChanged: (v) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletEmailChanged(v)),
                            ),
                            SizedBox(height: 12.h),
                            _Field(
                              label: 'VAT No.',
                              controller: _vatCtr,
                              onChanged: (v) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletVatNoChanged(v)),
                            ),
                            SizedBox(height: 12.h),
                            _Field(
                              label: 'Credit Limit (LKR)',
                              controller: _creditCtr,
                              keyboardType:
                                  const TextInputType.numberWithOptions(
                                      decimal: true),
                              onChanged: (v) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletCreditLimitChanged(v)),
                            ),
                            SizedBox(height: 12.h),
                            _Field(
                              label: 'Remarks',
                              controller: _remarksCtr,
                              maxLines: 4,
                              onChanged: (v) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletRemarksChanged(v)),
                            ),
                            SizedBox(height: 12.h),
                            _DateField(
                              label: 'Owner Date of Birth',
                              controller: _dobCtr,
                              value: state.ownerDOB,
                              onPicked: (dt) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletOwnerDOBChanged(dt)),
                            ),
                            SizedBox(height: 12.h),
                            _Field(
                              label: 'Image URL',
                              controller: _imageCtr,
                              keyboardType: TextInputType.url,
                              onChanged: (v) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletImageChanged(v)),
                            ),
                            SizedBox(height: 12.h),
                            _ProvinceDropdown(
                              selected: state.provinceCode,
                              onChanged: (code) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletProvinceChanged(code)),
                            ),
                            SizedBox(height: 12.h),
                            _DistrictDropdown(
                              provinceCode: state.provinceCode,
                              selected: state.districtCode,
                              onChanged: (code) => context
                                  .read<CreateOutletBloc>()
                                  .add(OutletDistrictChanged(code)),
                            ),
                            SizedBox(height: 24.h),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
              _SubmitBar(state: state),
            ],
          ),
        );
      },
    );
  }
}

// ── App bar ───────────────────────────────────────────────────────────────────

class _AppBar extends StatelessWidget {
  const _AppBar({required this.onBack});
  final VoidCallback onBack;

  @override
  Widget build(BuildContext context) {
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
        child: Padding(
          padding: EdgeInsets.fromLTRB(8.w, 4.h, 16.w, 16.h),
          child: Row(
            children: [
              GestureDetector(
                onTap: onBack,
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
                    'ADD OUTLET',
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
                    'Register a new outlet on your route',
                    style: GoogleFonts.barlow(
                      fontSize: 11.sp,
                      color: Colors.white.withValues(alpha: 0.70),
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
}

// ── Section header ────────────────────────────────────────────────────────────

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({required this.number, required this.label});
  final String number;
  final String label;

  @override
  Widget build(BuildContext context) {
    return SliverToBoxAdapter(
      child: Container(
        margin: EdgeInsets.fromLTRB(16.w, 16.h, 16.w, 0),
        padding: EdgeInsets.symmetric(horizontal: 12.w, vertical: 10.h),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(10.r),
          border: Border(
            left: BorderSide(color: AppColors.primary, width: 3.w),
          ),
        ),
        child: Row(
          children: [
            Container(
              width: 22.r,
              height: 22.r,
              decoration: BoxDecoration(
                color: AppColors.primary,
                shape: BoxShape.circle,
              ),
              child: Center(
                child: Text(
                  number,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w800,
                    color: Colors.white,
                  ),
                ),
              ),
            ),
            SizedBox(width: 10.w),
            Text(
              label,
              style: GoogleFonts.barlowCondensed(
                fontSize: 13.sp,
                fontWeight: FontWeight.w700,
                letterSpacing: 2.0,
                color: AppColors.foreground,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Text field ────────────────────────────────────────────────────────────────

class _Field extends StatelessWidget {
  const _Field({
    required this.label,
    required this.controller,
    this.onChanged,
    this.keyboardType,
    this.maxLines = 1,
    this.error,
  });

  final String label;
  final TextEditingController controller;
  final ValueChanged<String>? onChanged;
  final TextInputType? keyboardType;
  final int maxLines;
  final String? error;

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
            letterSpacing: 0.5,
            color: error != null ? AppColors.error : AppColors.foregroundMuted,
          ),
        ),
        SizedBox(height: 5.h),
        TextField(
          controller: controller,
          onChanged: onChanged,
          keyboardType: keyboardType,
          maxLines: maxLines,
          style: GoogleFonts.barlow(
            fontSize: 14.sp,
            color: AppColors.foreground,
          ),
          decoration: InputDecoration(
            isDense: true,
            contentPadding:
                EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
            filled: true,
            fillColor: error != null
                ? AppColors.error.withValues(alpha: 0.04)
                : AppColors.surface,
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10.r),
              borderSide: BorderSide(color: AppColors.surfaceVariant),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10.r),
              borderSide: BorderSide(
                color: error != null
                    ? AppColors.error.withValues(alpha: 0.5)
                    : AppColors.surfaceVariant,
              ),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10.r),
              borderSide: BorderSide(color: AppColors.primary, width: 1.5),
            ),
            errorText: null,
          ),
        ),
        if (error != null) ...[
          SizedBox(height: 4.h),
          Text(error!,
              style: GoogleFonts.barlow(
                  fontSize: 11.sp, color: AppColors.error)),
        ],
      ],
    );
  }
}

// ── Date field ────────────────────────────────────────────────────────────────

class _DateField extends StatelessWidget {
  const _DateField({
    required this.label,
    required this.controller,
    required this.value,
    required this.onPicked,
  });

  final String label;
  final TextEditingController controller;
  final DateTime? value;
  final ValueChanged<DateTime?> onPicked;

  String _format(DateTime dt) =>
      '${dt.day.toString().padLeft(2, '0')}/${dt.month.toString().padLeft(2, '0')}/${dt.year}';

  @override
  Widget build(BuildContext context) {
    controller.text = value != null ? _format(value!) : '';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: GoogleFonts.barlowCondensed(
            fontSize: 11.sp,
            fontWeight: FontWeight.w700,
            letterSpacing: 0.5,
            color: AppColors.foregroundMuted,
          ),
        ),
        SizedBox(height: 5.h),
        GestureDetector(
          onTap: () async {
            final picked = await showDatePicker(
              context: context,
              initialDate: value ?? DateTime(1985),
              firstDate: DateTime(1920),
              lastDate: DateTime.now(),
              builder: (ctx, child) => Theme(
                data: Theme.of(ctx).copyWith(
                  colorScheme: ColorScheme.light(primary: AppColors.primary),
                ),
                child: child!,
              ),
            );
            onPicked(picked);
          },
          child: AbsorbPointer(
            child: TextField(
              controller: controller,
              readOnly: true,
              style: GoogleFonts.barlow(
                  fontSize: 14.sp, color: AppColors.foreground),
              decoration: InputDecoration(
                isDense: true,
                contentPadding:
                    EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
                filled: true,
                fillColor: AppColors.surface,
                suffixIcon: Icon(Icons.calendar_today_rounded,
                    size: 16.r, color: AppColors.foregroundMuted),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10.r),
                  borderSide: BorderSide(color: AppColors.surfaceVariant),
                ),
                enabledBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10.r),
                  borderSide: BorderSide(color: AppColors.surfaceVariant),
                ),
                focusedBorder: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(10.r),
                  borderSide:
                      BorderSide(color: AppColors.primary, width: 1.5),
                ),
                hintText: 'DD/MM/YYYY',
                hintStyle: GoogleFonts.barlow(
                    fontSize: 14.sp, color: AppColors.foregroundMuted),
              ),
            ),
          ),
        ),
      ],
    );
  }
}

// ── Segmented selector ────────────────────────────────────────────────────────

class _SegmentLabel extends StatelessWidget {
  const _SegmentLabel(this.text);
  final String text;

  @override
  Widget build(BuildContext context) {
    return Text(
      text,
      style: GoogleFonts.barlowCondensed(
        fontSize: 11.sp,
        fontWeight: FontWeight.w700,
        letterSpacing: 0.5,
        color: AppColors.foregroundMuted,
      ),
    );
  }
}

class _SegmentedSelector extends StatelessWidget {
  const _SegmentedSelector({
    required this.options,
    required this.selected,
    required this.onSelect,
    this.error,
  });

  final List<String> options;
  final String selected;
  final ValueChanged<String> onSelect;
  final String? error;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: 8.w,
          runSpacing: 6.h,
          children: options.map((opt) {
            final isSelected = selected == opt;
            return GestureDetector(
              onTap: () => onSelect(opt),
              child: Container(
                padding:
                    EdgeInsets.symmetric(horizontal: 14.w, vertical: 8.h),
                decoration: BoxDecoration(
                  color: isSelected
                      ? AppColors.primary.withValues(alpha: 0.12)
                      : AppColors.surfaceVariant.withValues(alpha: 0.5),
                  borderRadius: BorderRadius.circular(8.r),
                  border: Border.all(
                    color: isSelected
                        ? AppColors.primary
                        : AppColors.surfaceVariant,
                    width: isSelected ? 1.5 : 1,
                  ),
                ),
                child: Text(
                  opt,
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 12.sp,
                    fontWeight: FontWeight.w700,
                    letterSpacing: 0.5,
                    color: isSelected
                        ? AppColors.primary
                        : AppColors.foregroundMuted,
                  ),
                ),
              ),
            );
          }).toList(),
        ),
        if (error != null) ...[
          SizedBox(height: 4.h),
          Text(error!,
              style:
                  GoogleFonts.barlow(fontSize: 11.sp, color: AppColors.error)),
        ],
      ],
    );
  }
}

// ── Province & district dropdowns ─────────────────────────────────────────────

class _ProvinceDropdown extends StatelessWidget {
  const _ProvinceDropdown(
      {required this.selected, required this.onChanged});
  final int? selected;
  final ValueChanged<int?> onChanged;

  @override
  Widget build(BuildContext context) {
    return _DropdownField<int>(
      label: 'Province',
      value: selected,
      items: kProvinces
          .map((p) => DropdownMenuItem(value: p.code, child: Text(p.name)))
          .toList(),
      onChanged: onChanged,
    );
  }
}

class _DistrictDropdown extends StatelessWidget {
  const _DistrictDropdown({
    required this.provinceCode,
    required this.selected,
    required this.onChanged,
  });
  final int? provinceCode;
  final int? selected;
  final ValueChanged<int?> onChanged;

  @override
  Widget build(BuildContext context) {
    final districts = provinceCode == null
        ? <SLDistrict>[]
        : kDistricts
            .where((d) => d.provinceCode == provinceCode)
            .toList();

    return _DropdownField<int>(
      label: 'District',
      value: selected,
      hint: provinceCode == null ? 'Select a province first' : 'Select district',
      enabled: provinceCode != null,
      items: districts
          .map((d) => DropdownMenuItem(value: d.code, child: Text(d.name)))
          .toList(),
      onChanged: onChanged,
    );
  }
}

class _DropdownField<T> extends StatelessWidget {
  const _DropdownField({
    required this.label,
    required this.value,
    required this.items,
    required this.onChanged,
    this.hint,
    this.enabled = true,
  });

  final String label;
  final T? value;
  final List<DropdownMenuItem<T>> items;
  final ValueChanged<T?> onChanged;
  final String? hint;
  final bool enabled;

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
            letterSpacing: 0.5,
            color: AppColors.foregroundMuted,
          ),
        ),
        SizedBox(height: 5.h),
        DropdownButtonFormField<T>(
          initialValue: value,
          items: items,
          onChanged: enabled ? onChanged : null,
          hint: Text(
            hint ?? 'Select',
            style: GoogleFonts.barlow(
                fontSize: 14.sp, color: AppColors.foregroundMuted),
          ),
          style: GoogleFonts.barlow(
              fontSize: 14.sp, color: AppColors.foreground),
          decoration: InputDecoration(
            isDense: true,
            contentPadding:
                EdgeInsets.symmetric(horizontal: 12.w, vertical: 12.h),
            filled: true,
            fillColor:
                enabled ? AppColors.surface : AppColors.surfaceVariant,
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10.r),
              borderSide: BorderSide(color: AppColors.surfaceVariant),
            ),
            enabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10.r),
              borderSide: BorderSide(color: AppColors.surfaceVariant),
            ),
            focusedBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10.r),
              borderSide: BorderSide(color: AppColors.primary, width: 1.5),
            ),
            disabledBorder: OutlineInputBorder(
              borderRadius: BorderRadius.circular(10.r),
              borderSide: BorderSide(
                  color: AppColors.surfaceVariant.withValues(alpha: 0.5)),
            ),
          ),
        ),
      ],
    );
  }
}

// ── Submit bar ────────────────────────────────────────────────────────────────

class _SubmitBar extends StatelessWidget {
  const _SubmitBar({required this.state});
  final CreateOutletState state;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 24.h),
      decoration: BoxDecoration(
        color: AppColors.background,
        border: Border(
            top: BorderSide(color: AppColors.surfaceVariant)),
      ),
      child: SizedBox(
        width: double.infinity,
        height: 52.h,
        child: ElevatedButton(
          onPressed: state.canSubmit
              ? () => context
                  .read<CreateOutletBloc>()
                  .add(const CreateOutletSubmitRequested())
              : null,
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.primary,
            disabledBackgroundColor: AppColors.surfaceVariant,
            shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12.r)),
            elevation: 0,
          ),
          child: state.isSubmitting
              ? SizedBox(
                  width: 20.r,
                  height: 20.r,
                  child: CircularProgressIndicator(
                      strokeWidth: 2, color: Colors.white),
                )
              : Text(
                  'REGISTER OUTLET',
                  style: GoogleFonts.barlowCondensed(
                    fontSize: 16.sp,
                    fontWeight: FontWeight.w800,
                    letterSpacing: 1.5,
                    color: state.canSubmit
                        ? Colors.white
                        : AppColors.foregroundMuted,
                  ),
                ),
        ),
      ),
    );
  }
}
