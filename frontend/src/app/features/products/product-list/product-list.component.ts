import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../../core/services/product.service';
import { CategoryService } from '../../../core/services/category.service';
import { AuthService } from '../../../core/services/auth.service';
import { Product, Category, CreateProductRequest, UpdateProductRequest } from '../../../core/models/models';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="page">
      <div class="page-header">
        <div>
          <h1>📦 Products</h1>
          <p class="subtitle">{{ filteredProducts().length }} products found</p>
        </div>
        @if (auth.isAdmin()) {
          <button class="btn-primary" (click)="openCreate()">+ Add Product</button>
        }
      </div>

      <!-- Filters -->
      <div class="filters">
        <input type="text" [(ngModel)]="searchTerm" (ngModelChange)="onSearch()" placeholder="🔍 Search by name or SKU..." class="search-input" />
        <select [(ngModel)]="selectedCategoryId" (ngModelChange)="loadProducts()" class="select-filter">
          <option value="">All Categories</option>
          @for (cat of categories(); track cat.id) {
            <option [value]="cat.id">{{ cat.name }}</option>
          }
        </select>
      </div>

      @if (loading()) {
        <div class="loading">Loading products...</div>
      } @else if (error()) {
        <div class="alert error">{{ error() }}</div>
      } @else {
        <div class="product-grid">
          @for (product of filteredProducts(); track product.id) {
            <div class="product-card" [class.inactive]="!product.isActive">
              <div class="product-header">
                <span class="category-tag">{{ product.categoryName }}</span>
                <span class="sku">{{ product.sku }}</span>
              </div>
              <h3 class="product-name">{{ product.name }}</h3>
              <p class="product-desc">{{ product.description }}</p>
              <div class="product-footer">
                <span class="price">\${{ product.price.toFixed(2) }}</span>
                <span class="stock" [class.low]="product.stockQuantity < 10">
                  {{ product.stockQuantity }} in stock
                </span>
              </div>
              @if (auth.isAdmin()) {
                <div class="actions">
                  <button class="btn-edit" (click)="openEdit(product)">Edit</button>
                  <button class="btn-delete" (click)="deleteProduct(product)">Delete</button>
                </div>
              }
            </div>
          }
          @empty {
            <div class="empty">No products found. {{ auth.isAdmin() ? 'Click "+ Add Product" to get started.' : '' }}</div>
          }
        </div>
      }

      <!-- Modal -->
      @if (showModal()) {
        <div class="modal-overlay" (click)="closeModal()">
          <div class="modal" (click)="$event.stopPropagation()">
            <h2>{{ editingProduct() ? 'Edit Product' : 'New Product' }}</h2>
            @if (modalError()) { <div class="alert error">{{ modalError() }}</div> }

            <div class="form-group">
              <label>Name *</label>
              <input [(ngModel)]="form.name" placeholder="Product name" />
            </div>
            <div class="form-group">
              <label>Category *</label>
              <select [(ngModel)]="form.categoryId">
                <option value="">Select category</option>
                @for (cat of categories(); track cat.id) {
                  <option [value]="cat.id">{{ cat.name }}</option>
                }
              </select>
            </div>
            <div class="form-row">
              <div class="form-group">
                <label>Price *</label>
                <input type="number" [(ngModel)]="form.price" min="0" step="0.01" placeholder="0.00" />
              </div>
              <div class="form-group">
                <label>Stock *</label>
                <input type="number" [(ngModel)]="form.stockQuantity" min="0" placeholder="0" />
              </div>
            </div>
            <div class="form-group">
              <label>SKU</label>
              <input [(ngModel)]="form.sku" placeholder="Optional — auto-generated if empty" />
            </div>
            <div class="form-group">
              <label>Description</label>
              <textarea [(ngModel)]="form.description" rows="3" placeholder="Product description..."></textarea>
            </div>
            @if (editingProduct()) {
              <div class="form-group toggle-row">
                <label>Active</label>
                <input type="checkbox" [(ngModel)]="form.isActive" />
              </div>
            }
            <div class="modal-actions">
              <button class="btn-secondary" (click)="closeModal()">Cancel</button>
              <button class="btn-primary" (click)="save()" [disabled]="saving()">
                {{ saving() ? 'Saving...' : 'Save' }}
              </button>
            </div>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .page { padding: 2rem; max-width: 1400px; margin: 0 auto; }
    .page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 1.5rem; }
    h1 { color: #e94560; margin: 0; font-size: 2rem; }
    .subtitle { color: #888; margin: .25rem 0 0; font-size: .9rem; }
    .filters { display: flex; gap: 1rem; margin-bottom: 2rem; flex-wrap: wrap; }
    .search-input, .select-filter { padding: .6rem 1rem; background: #16213e; border: 1px solid #333; border-radius: 6px; color: #fff; font-size: .9rem; outline: none; }
    .search-input { flex: 1; min-width: 200px; }
    .search-input:focus, .select-filter:focus { border-color: #e94560; }
    .product-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 1.5rem; }
    .product-card { background: #1a1a2e; border-radius: 10px; padding: 1.25rem; border: 1px solid #2a2a4a; transition: transform .2s, box-shadow .2s; }
    .product-card:hover { transform: translateY(-3px); box-shadow: 0 8px 24px rgba(233,69,96,.15); }
    .product-card.inactive { opacity: .5; }
    .product-header { display: flex; justify-content: space-between; margin-bottom: .75rem; }
    .category-tag { background: rgba(233,69,96,.15); color: #e94560; font-size: .75rem; padding: 2px 8px; border-radius: 12px; font-weight: 600; }
    .sku { color: #666; font-size: .75rem; font-family: monospace; }
    .product-name { color: #fff; margin: 0 0 .5rem; font-size: 1rem; line-height: 1.4; }
    .product-desc { color: #888; font-size: .85rem; margin: 0 0 1rem; line-height: 1.5; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
    .product-footer { display: flex; justify-content: space-between; align-items: center; margin-bottom: .75rem; }
    .price { color: #4ade80; font-size: 1.2rem; font-weight: 700; }
    .stock { color: #888; font-size: .85rem; }
    .stock.low { color: #f59e0b; }
    .actions { display: flex; gap: .5rem; padding-top: .75rem; border-top: 1px solid #2a2a4a; }
    .btn-primary { background: #e94560; color: white; border: none; padding: .5rem 1.25rem; border-radius: 6px; cursor: pointer; font-weight: 600; font-size: .9rem; transition: background .2s; }
    .btn-primary:hover:not(:disabled) { background: #c73652; }
    .btn-primary:disabled { opacity: .6; cursor: not-allowed; }
    .btn-secondary { background: transparent; color: #ccc; border: 1px solid #444; padding: .5rem 1.25rem; border-radius: 6px; cursor: pointer; font-size: .9rem; }
    .btn-edit { background: #3b82f6; color: white; border: none; padding: .35rem .75rem; border-radius: 4px; cursor: pointer; font-size: .8rem; flex: 1; }
    .btn-delete { background: #ef4444; color: white; border: none; padding: .35rem .75rem; border-radius: 4px; cursor: pointer; font-size: .8rem; flex: 1; }
    .loading { color: #888; text-align: center; padding: 3rem; font-size: 1.1rem; }
    .empty { color: #666; text-align: center; padding: 3rem; grid-column: 1/-1; }
    .alert { padding: .75rem 1rem; border-radius: 6px; margin-bottom: 1rem; font-size: .9rem; }
    .alert.error { background: rgba(233,69,96,.15); border: 1px solid #e94560; color: #e94560; }
    .modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,.7); display: flex; align-items: center; justify-content: center; z-index: 100; padding: 1rem; }
    .modal { background: #1a1a2e; border-radius: 12px; padding: 2rem; width: 100%; max-width: 520px; max-height: 90vh; overflow-y: auto; border: 1px solid #2a2a4a; }
    .modal h2 { color: #e94560; margin: 0 0 1.5rem; }
    .form-group { margin-bottom: 1rem; }
    .form-row { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    label { display: block; color: #ccc; font-size: .85rem; margin-bottom: .35rem; }
    input, select, textarea { width: 100%; padding: .6rem .9rem; background: #16213e; border: 1px solid #333; border-radius: 6px; color: #fff; font-size: .9rem; box-sizing: border-box; outline: none; font-family: inherit; }
    input:focus, select:focus, textarea:focus { border-color: #e94560; }
    textarea { resize: vertical; }
    .toggle-row { display: flex; align-items: center; gap: 1rem; }
    .toggle-row input { width: auto; }
    .modal-actions { display: flex; gap: 1rem; margin-top: 1.5rem; justify-content: flex-end; }
  `]
})
export class ProductListComponent implements OnInit {
  private productService = inject(ProductService);
  private categoryService = inject(CategoryService);
  auth = inject(AuthService);

  products = signal<Product[]>([]);
  categories = signal<Category[]>([]);
  loading = signal(true);
  error = signal('');
  searchTerm = '';
  selectedCategoryId = '';
  showModal = signal(false);
  editingProduct = signal<Product | null>(null);
  saving = signal(false);
  modalError = signal('');

  form: any = { name: '', description: '', price: 0, stockQuantity: 0, sku: '', categoryId: '', isActive: true };

  filteredProducts = computed(() => {
    const term = this.searchTerm.toLowerCase();
    if (!term) return this.products();
    return this.products().filter(p =>
      p.name.toLowerCase().includes(term) || p.sku.toLowerCase().includes(term)
    );
  });

  ngOnInit() {
    this.categoryService.getCategories().subscribe({ next: cats => this.categories.set(cats) });
    this.loadProducts();
  }

  loadProducts() {
    this.loading.set(true);
    this.productService.getProducts(this.selectedCategoryId || undefined).subscribe({
      next: prods => { this.products.set(prods); this.loading.set(false); },
      error: () => { this.error.set('Failed to load products.'); this.loading.set(false); }
    });
  }

  onSearch() {}

  openCreate() {
    this.editingProduct.set(null);
    this.form = { name: '', description: '', price: 0, stockQuantity: 0, sku: '', categoryId: this.categories()[0]?.id || '', isActive: true };
    this.modalError.set('');
    this.showModal.set(true);
  }

  openEdit(p: Product) {
    this.editingProduct.set(p);
    this.form = { name: p.name, description: p.description, price: p.price, stockQuantity: p.stockQuantity, sku: p.sku, categoryId: p.categoryId, isActive: p.isActive };
    this.modalError.set('');
    this.showModal.set(true);
  }

  closeModal() { this.showModal.set(false); }

  save() {
    if (!this.form.name || !this.form.categoryId) {
      this.modalError.set('Name and Category are required.');
      return;
    }
    this.saving.set(true);
    this.modalError.set('');
    const editing = this.editingProduct();

    if (editing) {
      const req: UpdateProductRequest = { ...this.form };
      this.productService.updateProduct(editing.id, req).subscribe({
        next: () => { this.loadProducts(); this.closeModal(); this.saving.set(false); },
        error: (err) => { this.modalError.set(err.error?.detail || 'Update failed.'); this.saving.set(false); }
      });
    } else {
      const req: CreateProductRequest = { ...this.form };
      this.productService.createProduct(req).subscribe({
        next: () => { this.loadProducts(); this.closeModal(); this.saving.set(false); },
        error: (err) => { this.modalError.set(err.error?.detail || err.error?.message || 'Create failed.'); this.saving.set(false); }
      });
    }
  }

  deleteProduct(p: Product) {
    if (!confirm(`Delete "${p.name}"?`)) return;
    this.productService.deleteProduct(p.id).subscribe({
      next: () => this.loadProducts(),
      error: () => this.error.set('Delete failed.')
    });
  }
}
