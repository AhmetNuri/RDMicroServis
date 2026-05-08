<?php
namespace App\Models;
use MongoDB\Laravel\Eloquent\Model;
class Product extends Model {
    protected $connection = 'mongodb';
    protected $collection = 'products';
    protected $fillable = [
        'name','description','category','price','stock_quantity',
        'images','tags','metadata','is_active','sku',
    ];
    protected $casts = [
        'price'          => 'float',
        'stock_quantity' => 'integer',
        'images'         => 'array',
        'tags'           => 'array',
        'metadata'       => 'array',
        'is_active'      => 'boolean',
    ];
    protected $attributes = [
        'is_active'      => true,
        'stock_quantity' => 0,
    ];
    public function scopeActive($query) { return $query->where('is_active', true); }
    public function scopeByCategory($query, string $cat) { return $query->where('category', $cat); }
}
