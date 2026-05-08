<?php
use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;
return new class extends Migration {
    public function up(): void {
        Schema::create('payments', function (Blueprint $t) {
            $t->uuid('id')->primary();
            $t->uuid('order_id')->index();
            $t->uuid('user_id')->nullable()->index();
            $t->decimal('amount',10,2);
            $t->string('currency',3)->default('USD');
            $t->enum('status',['pending','processing','completed','failed','refunded'])->default('pending');
            $t->string('payment_method')->default('card');
            $t->string('transaction_id')->nullable();
            $t->jsonb('metadata')->nullable();
            $t->timestamps();
        });
    }
    public function down(): void { Schema::dropIfExists('payments'); }
};
