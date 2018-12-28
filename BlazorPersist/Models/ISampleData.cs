using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorPersist.Models
{
	public interface ISampleData
	{
		int Id { get; }
		string Name { get; set; }
		string NickName { get; set; }
	}
}
