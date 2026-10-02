import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CategoryService } from '../../../core/services/category.service';
import { AuthService } from '../../../core/services/auth.service';
import { Category, CreateCategoryRequest, UpdateCategoryRequest } from '../../../core/models/models';

@Component({
  selector: 'app-category-list',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="page">
      <div class="page-header">
        <div>
          <h1>🗂️ Categories</h1>
          <p class="subtitle">{{ categories().length }} categories</p>
        </div>
        @if (auth.isAdmin()) {
          <button class="btn-primary" (click)="openCreate()">+ Add Category</button>
        }
      </div>

      @if (loading()) {
        <div class="loading">Loading categories...</div>
      } @else if (error()) {
        <div class="alert error">{{ error() }}</div>
      } @else {
        <div class="category-grid">
          @for (cat of categories(); track cat.id) {
            <div class="category-card">
              <div class="card-icon">🏷️</div>
              <div class="card-body">
                <h3>{{ cat.name }}</h3>
                <p>{{ cat.description || 'No description' }}</p>
                <div class="product-count">
                  <span class="badge">{{ cat.productsCount }} products</span>
                </div>
              </div>
              @if (auth.isAdmin()) {
                <div class="card-actions">
                  <button class="btn-edit" (click)="openEdit(cat)">Edit</button>
                  <button class="btn-delete" (click)="deleteCategory(cat)">Delete</button>
                </div>
              }
            </div>
          }
          @empty {
            <div class="empty">No categories yet.</div>
          }
        </div>
      }

      @if (showModal()) {
        <div class="modal-overlay" (click)="closeModal()">
          <div class="modal" (click)="$event.stopPropagation()">
            <h2>{{ editingCategory() ? 'Edit Category' : 'New Category' }}</h2>
            @if (modalError()) { <div class="alert error">{{ modalError() }}</div> }
            <div class="form-group">
              <label>Name *</label>
              <input [(ngModel)]="form.name" placeholder="Category name" />
            </div>
            <div class="form-group">
              <label>Description</label>
              <textarea [(ngModel)]="form.description" rows="3" placeholder="Optional description..."></textarea>
            </div>
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
    .page { padding: 2rem; max-width: 1200px; margin: 0 auto; }
    .page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 2rem; }
    h1 { color: #e94560; margin: 0; font-size: 2rem; }
    .subtitle { color: #888; margin: .25rem 0 0; font-size: .9rem; }
    .category-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 1.5rem; }
    .category-card { background: #1a1a2e; border-radius: 10px; padding: 1.5rem; border: 1px solid #2a2a4a; display: flex; flex-direction: column; gap: .75rem; transition: transform .2s; }
    .category-card:hover { transform: translateY(-3px); }
    .card-icon { font-size: 2rem; }
    .card-body h3 { color: #fff; margin: 0 0 .4rem; font-size: 1.1rem; }
    .card-body p { color: #888; margin: 0 0 .75rem; font-size: .85rem; line-height: 1.5; }
    .product-count .badge { background: rgba(233,69,96,.15); color: #e94560; font-size: .8rem; padding: 3px 10px; border-radius: 12px; font-weight: 600; }
    .card-actions { display: flex; gap: .5rem; padding-top: .75rem; border-top: 1px solid #2a2a4a; }
    .btn-primary { background: #e94560; color: white; border: none; padding: .5rem 1.25rem; border-radius: 6px; cursor: pointer; font-weight: 600; font-size: .9rem; }
    .btn-primary:hover:not(:disabled) { background: #c73652; }
    .btn-primary:disabled { opacity: .6; cursor: not-allowed; }
    .btn-secondary { background: transparent; color: #ccc; border: 1px solid #444; padding: .5rem 1.25rem; border-radius: 6px; cursor: pointer; font-size: .9rem; }
    .btn-edit { background: #3b82f6; color: white; border: none; padding: .35rem .75rem; border-radius: 4px; cursor: pointer; font-size: .8rem; flex: 1; }
    .btn-delete { background: #ef4444; color: white; border: none; padding: .35rem .75rem; border-radius: 4px; cursor: pointer; font-size: .8rem; flex: 1; }
    .loading { color: #888; text-align: center; padding: 3rem; }
    .empty { color: #666; text-align: center; padding: 3rem; grid-column: 1/-1; }
    .alert { padding: .75rem 1rem; border-radius: 6px; margin-bottom: 1rem; }
    .alert.error { background: rgba(233,69,96,.15); border: 1px solid #e94560; color: #e94560; }
    .modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,.7); display: flex; align-items: center; justify-content: center; z-index: 100; padding: 1rem; }
    .modal { background: #1a1a2e; border-radius: 12px; padding: 2rem; width: 100%; max-width: 440px; border: 1px solid #2a2a4a; }
    .modal h2 { color: #e94560; margin: 0 0 1.5rem; }
    .form-group { margin-bottom: 1rem; }
    label { display: block; color: #ccc; font-size: .85rem; margin-bottom: .35rem; }
    input, textarea { width: 100%; padding: .6rem .9rem; background: #16213e; border: 1px solid #333; border-radius: 6px; color: #fff; font-size: .9rem; box-sizing: border-box; outline: none; font-family: inherit; }
    input:focus, textarea:focus { border-color: #e94560; }
    textarea { resize: vertical; }
    .modal-actions { display: flex; gap: 1rem; margin-top: 1.5rem; justify-content: flex-end; }
  `]
})
export class CategoryListComponent implements OnInit {
  private categoryService = inject(CategoryService);
  auth = inject(AuthService);

  categories = signal<Category[]>([]);
  loading = signal(true);
  error = signal('');
  showModal = signal(false);
  editingCategory = signal<Category | null>(null);
  saving = signal(false);
  modalError = signal('');
  form = { name: '', description: '' };

  ngOnInit() { this.loadCategories(); }

  loadCategories() {
    this.loading.set(true);
    this.categoryService.getCategories().subscribe({
      next: cats => { this.categories.set(cats); this.loading.set(false); },
      error: () => { this.error.set('Failed to load categories.'); this.loading.set(false); }
    });
  }

  openCreate() {
    this.editingCategory.set(null);
    this.form = { name: '', description: '' };
    this.modalError.set('');
    this.showModal.set(true);
  }

  openEdit(c: Category) {
    this.editingCategory.set(c);
    this.form = { name: c.name, description: c.description };
    this.modalError.set('');
    this.showModal.set(true);
  }

  closeModal() { this.showModal.set(false); }

  save() {
    if (!this.form.name.trim()) { this.modalError.set('Category name is required.'); return; }
    this.saving.set(true);
    const editing = this.editingCategory();

    if (editing) {
      this.categoryService.updateCategory(editing.id, this.form as UpdateCategoryRequest).subscribe({
        next: () => { this.loadCategories(); this.closeModal(); this.saving.set(false); },
        error: (err) => { this.modalError.set(err.error?.message || 'Update failed.'); this.saving.set(false); }
      });
    } else {
      this.categoryService.createCategory(this.form as CreateCategoryRequest).subscribe({
        next: () => { this.loadCategories(); this.closeModal(); this.saving.set(false); },
        error: (err) => { this.modalError.set(err.error?.message || 'Create failed.'); this.saving.set(false); }
      });
    }
  }

  deleteCategory(c: Category) {
    if (!confirm(`Delete "${c.name}"? This will fail if it has products.`)) return;
    this.categoryService.deleteCategory(c.id).subscribe({
      next: () => this.loadCategories(),
      error: (err) => this.error.set(err.error?.message || 'Delete failed.')
    });
  }
}
