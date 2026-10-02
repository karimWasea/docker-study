export interface Category {
  id: string;
  name: string;
  description: string;
  createdAtUtc: string;
  productsCount: number;
}

export interface CategoryDetails extends Category {
  products: ProductSummary[];
}

export interface ProductSummary {
  id: string;
  name: string;
  price: number;
  stockQuantity: number;
  sku: string;
  isActive: boolean;
}

export interface Product {
  id: string;
  categoryId: string;
  categoryName: string;
  name: string;
  description: string;
  price: number;
  stockQuantity: number;
  sku: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CreateProductRequest {
  categoryId: string;
  name: string;
  description?: string;
  price: number;
  stockQuantity: number;
  sku?: string;
}

export interface UpdateProductRequest {
  categoryId: string;
  name: string;
  description?: string;
  price: number;
  stockQuantity: number;
  isActive?: boolean;
}

export interface CreateCategoryRequest {
  name: string;
  description?: string;
}

export interface UpdateCategoryRequest {
  name: string;
  description?: string;
}

export interface AuthResponse {
  token: string;
  expiresAtUtc: string;
  email: string;
  roles: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface DashboardStats {
  totalCustomers: number;
  totalOrders: number;
  totalRevenue: number;
  averageOrderValue: number;
  pendingOrders: number;
  completedOrders: number;
  topCustomers: TopCustomer[];
  generatedAtUtc: string;
}

export interface TopCustomer {
  customerId: string;
  customerName: string;
  ordersCount: number;
  totalSpent: number;
}

export interface Customer {
  id: string;
  name: string;
  email: string;
  createdAtUtc: string;
  ordersCount: number;
  totalSpent: number;
}

export interface Order {
  id: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  totalAmount: number;
  status: string;
  description: string;
  orderDateUtc: string;
}
