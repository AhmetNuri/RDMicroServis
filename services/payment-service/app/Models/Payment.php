<?php
namespace App\Models;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Concerns\HasUuids;
class Payment extends Model {
    use HasUuids;
    protected $table = 'payments';
    protected $fillable = ['order_id','user_id','amount','currency','status','payment_method','transaction_id','metadata'];
    protected $casts = ['amount'=>'decimal:2','metadata'=>'array'];
}
