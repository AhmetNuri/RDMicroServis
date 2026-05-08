<?php
namespace App\Http\Requests;
use Illuminate\Foundation\Http\FormRequest;
class UpdateProductRequest extends FormRequest {
    public function authorize(): bool { return true; }
    public function rules(): array {
        return [
            'name'           => 'sometimes|string|max:255',
            'description'    => 'sometimes|string',
            'category'       => 'sometimes|string|max:100',
            'price'          => 'sometimes|numeric|min:0',
            'stock_quantity' => 'sometimes|integer|min:0',
            'sku'            => 'nullable|string|max:100',
            'images'         => 'nullable|array',
            'tags'           => 'nullable|array',
            'metadata'       => 'nullable|array',
            'is_active'      => 'sometimes|boolean',
        ];
    }
}
