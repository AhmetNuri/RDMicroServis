<?php
namespace App\Models;
use Illuminate\Database\Eloquent\Model;
use Illuminate\Database\Eloquent\Concerns\HasUuids;
class OutboxMessage extends Model {
    use HasUuids;
    protected $table = 'outbox_messages';
    public $timestamps = false;
    protected $fillable = ['event_type','payload','retry_count','created_at'];
    protected $casts = ['processed_at'=>'datetime','created_at'=>'datetime'];
    public function scopeUnprocessed($query) { return $query->whereNull('processed_at')->where('retry_count','<',3); }
}
