<?php
use App\Http\Controllers\PaymentController;
use Illuminate\Support\Facades\Route;
Route::get('/health', fn() => response()->json(['status'=>'ok','service'=>'payment-service']));
Route::middleware('auth.jwt')->group(function () {
    Route::post('/payments', [PaymentController::class, 'store']);
    Route::get('/payments/{id}', [PaymentController::class, 'show']);
    Route::post('/payments/{id}/retry', [PaymentController::class, 'retry']);
    Route::get('/payments/order/{orderId}', [PaymentController::class, 'byOrder']);
});
