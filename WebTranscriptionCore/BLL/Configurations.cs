using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace WebTranscriptionCore {
	public class Configurations {

		protected readonly IConfiguration cfg;
		protected readonly long uId;

		[Cfg(ScopeCfg.Global)] public string Storage_Path { get; set; }
		[Cfg(ScopeCfg.Global)] public string Editor_Global_PlayPause { get; set; } = @"F5";
		[Cfg(ScopeCfg.Global)] public string Editor_Global_Faster { get; set; } = @"F10";
		[Cfg(ScopeCfg.Global)] public string Editor_Global_Slower { get; set; } = @"F9";
		[Cfg(ScopeCfg.Global)] public string Editor_Global_Backward { get; set; } = @"F6";
		[Cfg(ScopeCfg.Global)] public string Editor_Global_Forward { get; set; } = @"F7";
		[Cfg(ScopeCfg.Global)] public TimeSpan Editor_Global_PauseRewind { get; set; } = TimeSpan.FromSeconds(2);
		[Cfg(ScopeCfg.Global)] public string Login_Ldap_Server { get; set; }
		[Cfg(ScopeCfg.Global)] public string Login_Ldap_QueryUser { get; set; }
		[Cfg(ScopeCfg.Global)] public string Login_Ldap_QueryPassword { get; set; }
		[Cfg(ScopeCfg.Global)] public string Login_Ldap_DomainSID { get; set; }
		[Cfg(ScopeCfg.Global)] public string Global_Others_TranscriptionProvider { get; set; }
		[Cfg(ScopeCfg.Global)] public string Global_Others_OnlineUrl { get; set; }
		[Cfg(ScopeCfg.Global)] public string DigitroUser { get; set; }
		[Cfg(ScopeCfg.Global)] public string DigitroPassword { get; set; }
		[Cfg(ScopeCfg.Global)] public string Digitro_NewLine { get; set; } = "true";
		[Cfg(ScopeCfg.Global)] public string AutoTrancriptionByTask { get; set; } = "false";
		[Cfg(ScopeCfg.User)] public string Editor_PlayPause { get; set; } = null;
		[Cfg(ScopeCfg.User)] public string Editor_Backward { get; set; } = null;
		[Cfg(ScopeCfg.User)] public string Editor_Forward { get; set; } = null;
		[Cfg(ScopeCfg.User)] public string Editor_Slower { get; set; } = null;
		[Cfg(ScopeCfg.User)] public string Editor_Faster { get; set; } = null;
		[Cfg(ScopeCfg.User)] public TimeSpan? Editor_PauseRewind { get; set; } = null;

		public bool AutoTransc => Global_Others_TranscriptionProvider != "NenhumProvider";

		protected class CfgAttribute : Attribute {
			public ScopeCfg Scope { get; }
			public CfgAttribute(ScopeCfg scope) => Scope = scope;
		}

		public Configurations() { }

		public Configurations(IConfiguration cfg) => this.cfg = cfg;

		public Configurations(IConfiguration cfg, long uId) {
			this.cfg = cfg;
			this.uId = uId;
		}

		private IEnumerable<PropertyInfo> GetProps(ScopeCfg scope) => GetType().GetProperties().Where(p => p.Get<CfgAttribute>()?.Scope == scope);

		public void LoadConfig() {
			LoadConfigGlobal();
			LoadConfigUser();
		}


		//criar classe system configuration ou criar uma para o dapper em pasta especifica
		public void LoadConfigGlobal() {
			string sql = "SELECT NAME, VALUE FROM SystemConfiguration";
			List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();

			foreach (SystemConfiguration item in BLL.Cnn(cfg).Query<SystemConfiguration>(sql, new { }))
				list.Add(new KeyValuePair<string, string>(item.Name, item.Value));

			SetConfig(ScopeCfg.Global, list);
		}

		public void LoadConfigUser() {
			string sql = "SELECT NAME, VALUE FROM UserConfiguration WHERE UserId = :userId";
			List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();

			foreach (UserConfiguration item in BLL.Cnn(cfg).Query<UserConfiguration>(sql, new { userId = uId }))
				list.Add(new KeyValuePair<string, string>(item.Name, item.Value));

			SetConfig(ScopeCfg.User, list);
		}

		private void SetConfig(ScopeCfg scope, IEnumerable<KeyValuePair<string, string>> list) {
			IEnumerable<PropertyInfo> props = GetProps(scope);
			foreach (KeyValuePair<string, string> item in list) {
				PropertyInfo prop = props.FirstOrDefault(p => p.Name == item.Key);
				if (prop != null) {
					Type t = prop.PropertyType.IsGenericOf(typeof(Nullable<>)) ? prop.PropertyType.GetGenericArguments().First() : prop.PropertyType;
					if (t == typeof(TimeSpan))
						prop.SetValue(this, TimeSpan.Parse(item.Value), null);
					else
						prop.SetValue(this, item.Value, null);
				}
			}
		}
	}
}