<?php
use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;
return new class extends Migration {
    public function up(): void {
        Schema::create('outbox_messages', function (Blueprint $t) {
            $t->uuid('id')->primary();
            $t->string('event_type');
            $t->text('payload');
            $t->timestamp('processed_at')->nullable();
            $t->unsignedTinyInteger('retry_count')->default(0);
            $t->timestamp('created_at')->useCurrent();
        });
    }
    public function down(): void { Schema::dropIfExists('outbox_messages'); }
};
