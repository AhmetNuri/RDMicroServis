<?php
namespace App\Http\Controllers;
use App\Http\Requests\CreateProductRequest;
use App\Http\Requests\UpdateProductRequest;
use App\Http\Resources\ProductResource;
use App\Models\Product;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Log;

class ProductController extends Controller {
    public function index(Request $request): JsonResponse {
        $query = Product::active();
        if ($request->filled('category')) $query->byCategory($request->category);
        if ($request->filled('min_price')) $query->where('price', '>=', (float)$request->min_price);
        if ($request->filled('max_price')) $query->where('price', '<=', (float)$request->max_price);
        $perPage = min((int)$request->get('per_page', 20), 100);
        $products = $query->orderBy('created_at', 'desc')->paginate($perPage);
        return response()->json([
            'data'      => ProductResource::collection($products->items()),
            'total'     => $products->total(),
            'page'      => $products->currentPage(),
            'per_page'  => $products->perPage(),
            'last_page' => $products->lastPage(),
        ]);
    }

    public function show(string $id): JsonResponse {
        $product = Product::find($id);
        if (!$product) return response()->json(['error' => 'Product not found'], 404);
        return response()->json(new ProductResource($product));
    }

    public function search(Request $request): JsonResponse {
        $q = $request->get('q', '');
        if (empty($q)) return response()->json(['error' => 'Query required'], 422);
        $products = Product::where('$text', ['$search' => $q])->where('is_active', true)->limit(50)->get();
        return response()->json(['data' => ProductResource::collection($products), 'total' => $products->count()]);
    }

    public function store(CreateProductRequest $request): JsonResponse {
        $product = Product::create($request->validated());
        $this->publishKafkaEvent('product.created', $product->toArray());
        return response()->json(new ProductResource($product), 201);
    }

    public function update(UpdateProductRequest $request, string $id): JsonResponse {
        $product = Product::find($id);
        if (!$product) return response()->json(['error' => 'Product not found'], 404);
        $product->update($request->validated());
        $this->publishKafkaEvent('product.updated', $product->fresh()->toArray());
        return response()->json(new ProductResource($product->fresh()));
    }

    public function destroy(string $id): JsonResponse {
        $product = Product::find($id);
        if (!$product) return response()->json(['error' => 'Product not found'], 404);
        $product->update(['is_active' => false]);
        $this->publishKafkaEvent('product.deleted', (string)$id);
        return response()->json(null, 204);
    }

    private function publishKafkaEvent(string $topic, mixed $payload): void {
        try {
            Log::info("Kafka event: {$topic}", ['payload' => $payload]);
        } catch (\Throwable $e) {
            Log::error("Kafka publish failed: {$topic} - {$e->getMessage()}");
        }
    }
}
