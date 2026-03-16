"use client";

import { useState, useMemo, useEffect } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import { useManageItemsDialog } from "../../store";
import {
  useBulkUpdatePricingStructureItems,
  usePricingStructureItems,
} from "../../hooks/pricing-structure.hooks";
import { useAllActiveProducts } from "@/features/product/hooks/product.hooks";
import type { BulkUpdateItemsInput } from "../../schema/pricing-structure.schema";

type ItemPrices = { unitPrice: string; packPrice: string };

export function ManageItemsDialog() {
  const { isOpen, selectedId, close } = useManageItemsDialog();
  const { data: allProducts, isLoading: isLoadingProducts } =
    useAllActiveProducts();
  const { data: existingItems, isLoading: isLoadingItems } =
    usePricingStructureItems(selectedId);
  const { mutate, isPending, clearFieldErrors } =
    useBulkUpdatePricingStructureItems();

  const initialPrices = useMemo<Record<number, ItemPrices>>(() => {
    if (!existingItems) return {};
    const seed: Record<number, ItemPrices> = {};
    existingItems.forEach((item) => {
      seed[item.productId] = {
        unitPrice: String(item.unitPrice),
        packPrice: item.packPrice != null ? String(item.packPrice) : "",
      };
    });
    return seed;
  }, [existingItems]);

  const [prices, setPrices] =
    useState<Record<number, ItemPrices>>(initialPrices);

  useEffect(() => {
    setPrices(initialPrices);
  }, [initialPrices]);

  const handleClose = () => {
    close();
    clearFieldErrors();
    setPrices({});
  };

  const handleSave = () => {
    if (!selectedId || !allProducts) return;
    const items: BulkUpdateItemsInput["items"] = allProducts
      .map((product) => {
        const p = prices[product.id];
        const unitPrice = p ? parseFloat(p.unitPrice) : NaN;
        const packPrice =
          p?.packPrice !== "" && p?.packPrice != null
            ? parseFloat(p.packPrice)
            : undefined;
        if (isNaN(unitPrice)) return null;
        return { productId: product.id, unitPrice, packPrice };
      })
      .filter((item): item is NonNullable<typeof item> => item !== null);

    mutate({ id: selectedId, data: { items } });
  };

  const isLoading = isLoadingProducts || isLoadingItems;

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && handleClose()}>
      <DialogContent className="w-[90vw] max-w-4xl min-w-4xl h-[90vh] flex flex-col overflow-hidden">
        <DialogHeader className="shrink-0">
          <DialogTitle>Manage Items</DialogTitle>
          <DialogDescription>
            Set unit and pack prices for each product. Leave unit price blank to
            exclude a product.
          </DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className="flex flex-1 items-center justify-center">
            <Spinner className="size-6" />
          </div>
        ) : (
          <>
            {/* Column headers */}
            <div className="grid grid-cols-[300px_200px_200px] gap-4 px-1 pb-2 border-b shrink-0">
              <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                Product
              </span>
              <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                Unit Price
              </span>
              <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                Pack Price
              </span>
            </div>

            {/* Scrollable product rows */}
            <div className="flex-1 overflow-y-auto min-h-0 divide-y">
              {allProducts?.map((product) => {
                const p = prices[product.id] ?? {
                  unitPrice: "",
                  packPrice: "",
                };
                return (
                  <div
                    key={product.id}
                    className="grid grid-cols-[300px_200px_200px] gap-4 py-3 items-center"
                  >
                    <div>
                      <p className="text-sm font-medium leading-none">
                        {product.itemDescription}
                      </p>
                      <p className="text-xs text-muted-foreground mt-0.5">
                        {product.code}
                      </p>
                    </div>
                    <Input
                      type="number"
                      step="0.01"
                      min="0"
                      placeholder="0.00"
                      value={p.unitPrice}
                      onChange={(e) =>
                        setPrices((prev) => ({
                          ...prev,
                          [product.id]: { ...p, unitPrice: e.target.value },
                        }))
                      }
                    />
                    <Input
                      type="number"
                      step="0.01"
                      min="0"
                      placeholder="Optional"
                      value={p.packPrice}
                      onChange={(e) =>
                        setPrices((prev) => ({
                          ...prev,
                          [product.id]: { ...p, packPrice: e.target.value },
                        }))
                      }
                    />
                  </div>
                );
              })}
            </div>

            {/* Sticky footer */}
            <div className="shrink-0 border-t pt-4 flex gap-2">
              <Button
                type="button"
                variant="outline"
                onClick={handleClose}
                className="flex-1"
              >
                Cancel
              </Button>
              <Button
                onClick={handleSave}
                disabled={isPending}
                className="flex-1"
              >
                {isPending && <Spinner className="mr-2" />}
                Save All
              </Button>
            </div>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
