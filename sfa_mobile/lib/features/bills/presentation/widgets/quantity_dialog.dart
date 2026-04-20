import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:uswatte/core/theme/app_theme.dart';
import 'package:uswatte/features/bills/data/datasources/bills_local_datasource.dart';

/// Prompts the rep for a quantity for [product]. Returns null if cancelled,
/// or the positive quantity otherwise.
Future<double?> showQuantityDialog(
  BuildContext context, {
  required ProductWithPrice product,
}) {
  return showDialog<double>(
    context: context,
    builder: (ctx) => _QuantityDialog(product: product),
  );
}

class _QuantityDialog extends StatefulWidget {
  final ProductWithPrice product;
  const _QuantityDialog({required this.product});

  @override
  State<_QuantityDialog> createState() => _QuantityDialogState();
}

class _QuantityDialogState extends State<_QuantityDialog> {
  final TextEditingController _controller = TextEditingController(text: '1');
  String? _error;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _submit() {
    final raw = _controller.text.trim();
    final value = double.tryParse(raw);
    if (value == null || value <= 0) {
      setState(() => _error = 'Enter a quantity greater than zero.');
      return;
    }
    Navigator.of(context).pop(value);
  }

  @override
  Widget build(BuildContext context) {
    final price = widget.product.dealerPackPrice;
    return AlertDialog(
      title: Text(
        widget.product.itemDescription,
        maxLines: 2,
        overflow: TextOverflow.ellipsis,
      ),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            widget.product.code,
            style: const TextStyle(
                color: AppColors.foregroundMuted, fontSize: 12),
          ),
          if (price != null) ...[
            const SizedBox(height: 4),
            Text(
              'Rs. ${price.toStringAsFixed(2)} per pack',
              style: const TextStyle(
                fontWeight: FontWeight.w600,
                color: AppColors.primary,
              ),
            ),
          ],
          const SizedBox(height: 16),
          TextField(
            controller: _controller,
            autofocus: true,
            keyboardType:
                const TextInputType.numberWithOptions(decimal: true),
            inputFormatters: [
              FilteringTextInputFormatter.allow(RegExp(r'[0-9.]')),
            ],
            decoration: InputDecoration(
              labelText: 'Quantity',
              errorText: _error,
              border: const OutlineInputBorder(),
            ),
            onSubmitted: (_) => _submit(),
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.of(context).pop(),
          child: const Text('Cancel'),
        ),
        FilledButton(
          onPressed: _submit,
          child: const Text('Add to Cart'),
        ),
      ],
    );
  }
}
