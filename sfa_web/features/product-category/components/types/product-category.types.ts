export type { ProductCategoryDto } from '../../schema/product-category.schema'

export type ProductCategoryTableMeta = {
  onEdit: (id: number) => void
  onActivate: (id: number) => void
  onDeactivate: (id: number) => void
}
