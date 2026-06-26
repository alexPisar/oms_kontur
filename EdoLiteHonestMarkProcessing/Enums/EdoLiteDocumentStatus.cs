using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdoLiteHonestMarkProcessing.Enums
{
    public enum EdoLiteDocumentStatus
    {
        /// <summary>
        /// Черновик
        /// </summary>
        Draft,

        /// <summary>
        /// Отправлен
        /// </summary>
        Sent,

        /// <summary>
        /// Доставлен (подпись не требуется)
        /// </summary>
        Delivered,

        /// <summary>
        /// Доставлен, ожидается подпись
        /// </summary>
        DeliveredAwaitingSignature,

        /// <summary>
        /// Подписан
        /// </summary>
        Signed,

        /// <summary>
        /// Отклонён
        /// </summary>
        Rejected,

        /// <summary>
        /// Уточнён
        /// </summary>
        Clarified = 7,

        /// <summary>
        /// Ожидается уточнение
        /// </summary>
        ClarificationPending,

        /// <summary>
        /// Ошибка в подписи
        /// </summary>
        SignatureError,

        /// <summary>
        /// Ошибка доставки
        /// </summary>
        DeliveryError,

        /// <summary>
        /// Ожидается отправка
        /// </summary>
        AwaitingDispatch,

        /// <summary>
        /// Просмотрен (подпись не требуется)
        /// </summary>
        Viewed,

        /// <summary>
        /// Просмотрен (ожидается подпись)
        /// </summary>
        ViewedAwaitingSignature,

        /// <summary>
        /// Требуется уточнение (запрос на уточнение просмотрен)
        /// </summary>
        ClarificationRequired,

        /// <summary>
        /// Отклонен (запрос просмотрен)
        /// </summary>
        RejectedReviewed,

        /// <summary>
        /// Ожидается аннулирование
        /// </summary>
        СancellationPending,

        /// <summary>
        /// Подписан и отправлен
        /// </summary>
        SignedAndSend = 61,

        /// <summary>
        /// Подписан, но не принят в ГИС МТ
        /// </summary>
        SignedNotReceived,

        /// <summary>
        /// Подписан, отправляется в ГИС МТ
        /// </summary>
        SignedSending
    }
}
