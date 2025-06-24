using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace EdiProcessingUnit.HonestMark
{
    public enum DocumentFormatsEnum
    {
        [EnumMember(Value = "NONE")]
        None = 0,

        [EnumMember(Value = "MANUAL")]
        Manual,

        [EnumMember(Value = "XML")]
        Xml,

        [EnumMember(Value = "CSV")]
        Csv
    }

    public enum ProductGroupsEnum
    {
        [EnumMember(Value = "none")]
        None = 0,

        /// <summary>
        ///Предметы одежды, бельё постельное, столовое, туалетное и кухонное
        /// </summary>
        [EnumMember(Value = "lp")]
        Lp,

        /// <summary>
        ///Обувные товары
        /// </summary>
        [EnumMember(Value = "shoes")]
        Shoes,

        /// <summary>
        ///Табачная продукция
        /// </summary>
        [EnumMember(Value = "tobacco")]
        Tobacco,

        /// <summary>
        ///Духи и туалетная вода
        /// </summary>
        [EnumMember(Value = "perfumery")]
        Perfumery,

        /// <summary>
        ///Шины и покрышки пневматические резиновые новые
        /// </summary>
        [EnumMember(Value = "tires")]
        Tires,

        /// <summary>
        ///Фотокамеры (кроме кинокамер), фотовспышки и лампы-вспышки
        /// </summary>
        [EnumMember(Value = "electronics")]
        Electronics,

        /// <summary>
        ///Молочная продукция
        /// </summary>
        [EnumMember(Value = "milk")]
        Milk = 8,

        /// <summary>
        ///Велосипеды и велосипедные рамы
        /// </summary>
        [EnumMember(Value = "bicycle")]
        Bicycle,

        /// <summary>
        ///Кресла-коляски
        /// </summary>
        [EnumMember(Value = "wheelchairs")]
        Wheelchairs,

        /// <summary>
        ///Альтернативная табачная продукция
        /// </summary>
        [EnumMember(Value = "otp")]
        Otp = 12,

        /// <summary>
        ///Упакованная вода
        /// </summary>
        [EnumMember(Value = "water")]
        Water,

        /// <summary>
        ///Товары из натурального меха
        /// </summary>
        [EnumMember(Value = "furs")]
        Furs,

        /// <summary>
        ///Пиво, напитки, изготавливаемые на основе пива, слабоалкогольные напитки
        /// </summary>
        [EnumMember(Value = "beer")]
        Beer,

        /// <summary>
        ///Никотиносодержащая продукция
        /// </summary>
        [EnumMember(Value = "ncp")]
        Ncp,

        /// <summary>
        ///Биологические активные добавки к пище
        /// </summary>
        [EnumMember(Value = "bio")]
        Bio
    }

    public enum DocumentProcessStatusesEnum
    {
        [EnumMember(Value = "none")]
        None = 0,

        /// <summary>
        /// Документ обрабатывается
        /// </summary>
        [EnumMember(Value = "IN_PROGRESS")]
        InProgress,

        /// <summary>
        /// Документ обработан с ошибками
        /// </summary>
        [EnumMember(Value = "CHECKED_NOT_OK")]
        CheckedNotOk,

        /// <summary>
        /// Документ обработан с ошибками
        /// </summary>
        [EnumMember(Value = "PARSE_ERROR")]
        ParseError,

        /// <summary>
        /// Техническая ошибка
        /// </summary>
        [EnumMember(Value = "PROCESSING_ERROR")]
        ProcessingError,

        /// <summary>
        /// Аннулирован
        /// </summary>
        [EnumMember(Value = "CANCELLED")]
        Cancelled,

        /// <summary>
        /// Ожидает регистрации участника в ГИС МТ
        /// </summary>
        [EnumMember(Value = "WAIT_PARTICIPANT_REGISTRATION")]
        WaitRarticipantRegistration,

        /// <summary>
        /// Ожидает продолжения обработки документа
        /// </summary>
        [EnumMember(Value = "WAIT_FOR_CONTINUATION")]
        WaitForContinuation,

        /// <summary>
        /// Ожидает приемку
        /// </summary>
        [EnumMember(Value = "WAIT_ACCEPTANCE")]
        WaitAcceptance,

        /// <summary>
        /// Документ успешно обработан
        /// </summary>
        [EnumMember(Value = "CHECKED_OK")]
        CheckedOk,

        /// <summary>
        /// Принят
        /// </summary>
        [EnumMember(Value = "ACCEPTED")]
        Accepted
    }

    public enum HonestMarkProcessResultStatus
    {
        /// <summary>
        /// Документ обработан  успешно
        /// </summary>
        SUCCESS,

        /// <summary>
        /// Документ обработан, но с ошибками
        /// </summary>
        FAILED,

        /// <summary>
        /// Документ в процессе обработки
        /// </summary>
        IN_PROGRESS
    }

    public enum DocEdoProcessingStatus
    {
        /// <summary>
        /// Новый
        /// </summary>
        New,

        /// <summary>
        /// Отправлен
        /// </summary>
        Sent,

        /// <summary>
        /// Обработан
        /// </summary>
        Processed,

        /// <summary>
        /// Ошибка обработки
        /// </summary>
        ProcessingError = 8,
    }

    public enum AnnulmentDocumentStatus
    {
        ///<summary>
        ///Отклонён
        ///</summary>
        Rejected = -1,

        ///<summary>
        ///Аннулирование отсутствует
        ///</summary>
        None,

        ///<summary>
        ///Запрошено аннулирование с нашей стороны
        ///</summary>
        Requested,

        ///<summary>
        ///Аннулирован, ожидает обработки в Честном знаке
        ///</summary>
        RevokedWaitProcessing,

        ///<summary>
        ///Аннулирован и обработан в Честном знаке
        ///</summary>
        RevokedAndProcessed,

        ///<summary>
        ///Аннулирован
        ///</summary>
        Revoked,

        ///<summary>
        ///Произошла ошибка
        ///</summary>
        Error = 8,
    }
}
