using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess
{
	public enum OrderStatus
	{
		Новый = 0,
		Экспортирован = 1,
		Отобран = 2,
		Отправлен = 3,
		Принят = 4,
	}
}
