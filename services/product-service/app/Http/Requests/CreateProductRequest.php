<?php
namespace App\Http\Requests;
use Illuminate\Foundation\Http\FormRequest;
class CreateProductRequest extends FormRequest {
    public function authorize(): bool { return true; }
    public function rules(): array {
        return [
            'name'           => 'required|string|max:255',
            'description'    => 'required|string',
            'category'       => 'required|string|max:100',
            'price'          => 'required|numeric|min:0',
            'stock_quantity' => 'required|integer|min:0',
            'sku'            => 'nullable|string|max:100',
            'images'         => 'nullable|array',
            'images.*'       => 'nullable|string',
            'tags'           => 'nullable|array',
            'tags.*'         => 'nullable|string',
            'metadata'       => 'nullable|array',
        ];
    }
}
