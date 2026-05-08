<?php
namespace App\Http\Resources;
use Illuminate\Http\Request;
use Illuminate\Http\Resources\Json\JsonResource;
class ProductResource extends JsonResource {
    public function toArray(Request $request): array {
        return [
            'id'             => (string) $this->_id,
            'name'           => $this->name,
            'description'    => $this->description,
            'category'       => $this->category,
            'price'          => $this->price,
            'stock_quantity' => $this->stock_quantity,
            'images'         => $this->images ?? [],
            'tags'           => $this->tags ?? [],
            'metadata'       => $this->metadata ?? [],
            'is_active'      => $this->is_active,
            'sku'            => $this->sku,
            'created_at'     => $this->created_at?->toISOString(),
            'updated_at'     => $this->updated_at?->toISOString(),
        ];
    }
}
