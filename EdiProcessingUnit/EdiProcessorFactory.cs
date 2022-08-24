using DataContextManagementUnit.DataAccess.Contexts.Edi;
using EdiProcessingUnit.Infrastructure;

namespace EdiProcessingUnit
{
	public class EdiProcessorFactory
	{
		private Edi.Edi _edi;
		private EdiDbContext _ediDbContext;
		private bool _isAuth = false;
        private string _organizationGln;

        public string OrganizationGln {
            set {
                _organizationGln = value;
                _edi.ResetParameters();
            }
        }
		
		public EdiProcessorFactory()
		{
			_edi = Edi.Edi.GetInstance();
			_ediDbContext = new EdiDbContext();
		}

		private void Auth()
		{			
			_isAuth = _edi.Authenticate(_organizationGln);
		}
		
		/// <summary>
		/// Метод создания и запуска указанного обработчика
		/// </summary>
		/// <param name="ediProcessor">Интерфейс обработчика EDI-сообщений определённого типа</param>
		public void RunProcessor(EdiProcessor ediProcessor)
		{
			// сначала аутентифицируемся.
			// делаем это перед каждым вызовом
			// на свякий случай
			Auth();

			if (_isAuth)
			{
				// если токен получен или предыдущий ещё не истёк,
				// то проинициализируем обработчик и запустим его
				// по-сути Init(...).Run() - Это фабричный метод 
				// всё работает так, чтобы на уровне вызова RunProcessor было красиво,
				// без лишних аргументов
				ediProcessor.Init( _edi, _ediDbContext ).Run();
			}
		}


		/// <summary>
		/// Метод создания указанного обработчика
		/// </summary>
		/// <param name="ediProcessor">Интерфейс обработчика EDI-сообщений определённого типа</param>
		/// <returns>Возвращает созданный и инициализированный обработчик или null, если что-то пошло не так.</returns>
		public EdiProcessor CreateProcessor(EdiProcessor ediProcessor, string ProcessorName = "default")
		{
			Auth();

			if (_isAuth)
			{
				EdiProcessor processor = ediProcessor.Init( _edi, _ediDbContext );
				processor.ProcessorName = ProcessorName;
				return processor;
			}

			return null;
		}
	}
}
