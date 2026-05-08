<?php
namespace App\Services;
use App\Models\Payment;
use App\Models\OutboxMessage;
use Illuminate\Support\Facades\DB;
use Illuminate\Support\Facades\Log;
use Illuminate\Support\Str;
class PaymentService {
    public function initiatePayment(array $data): Payment {
        return DB::transaction(function () use ($data) {
            $payment = Payment::create([
                'order_id'       => $data['order_id'],
                'user_id'        => $data['user_id'] ?? null,
                'amount'         => $data['amount'],
                'currency'       => $data['currency'] ?? 'USD',
                'status'         => 'processing',
                'payment_method' => $data['payment_method'] ?? 'card',
                'metadata'       => $data['metadata'] ?? null,
            ]);
            OutboxMessage::create(['event_type'=>'payment.initiated','payload'=>json_encode(['payment_id'=>$payment->id,'order_id'=>$payment->order_id,'amount'=>$payment->amount]),'retry_count'=>0,'created_at'=>now()]);
            // simulate 90% success
            $success = rand(1, 100) <= 90;
            $payment->status = $success ? 'completed' : 'failed';
            $payment->transaction_id = $success ? 'TXN-'.strtoupper(Str::random(12)) : null;
            $payment->save();
            $eventType = $success ? 'payment.completed' : 'payment.failed';
            OutboxMessage::create(['event_type'=>$eventType,'payload'=>json_encode(['payment_id'=>$payment->id,'order_id'=>$payment->order_id,'status'=>$payment->status,'amount'=>$payment->amount]),'retry_count'=>0,'created_at'=>now()]);
            return $payment;
        });
    }
    public function retryPayment(string $id): Payment {
        $payment = Payment::findOrFail($id);
        if ($payment->status !== 'failed') throw new \RuntimeException('Only failed payments can be retried');
        return $this->initiatePayment(['order_id'=>$payment->order_id,'user_id'=>$payment->user_id,'amount'=>$payment->amount,'currency'=>$payment->currency,'payment_method'=>$payment->payment_method]);
    }
    public function getPayment(string $id): Payment { return Payment::findOrFail($id); }
    public function getPaymentByOrder(string $orderId): ?Payment { return Payment::where('order_id',$orderId)->latest()->first(); }
    public function processOutbox(): void {
        OutboxMessage::unprocessed()->get()->each(function ($msg) {
            try {
                Log::info("Outbox: publishing {$msg->event_type}", ['payload'=>$msg->payload]);
                $msg->processed_at = now(); $msg->save();
            } catch (\Throwable $e) {
                $msg->increment('retry_count');
                Log::error("Outbox failed: {$msg->event_type} - {$e->getMessage()}");
            }
        });
    }
}
