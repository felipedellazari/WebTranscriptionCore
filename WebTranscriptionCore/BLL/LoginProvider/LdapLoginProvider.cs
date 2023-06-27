using Kenta.Utils;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace WebTranscriptionCore {
	public class LdapLoginProvider : BaseClass, ILoginProvider {

		private readonly string server, queryUser, queryPassword;
		private readonly SecurityIdentifier domainSid;

		public UserK UserK { get; set; }

		public string ErrorMsg { get; set; }

		public LdapLoginProvider(IConfiguration cfg) : base(cfg) {
			Configurations cfgs = new Configurations(cfg);
			cfgs.LoadConfigGlobal();
			server = cfgs.Login_Ldap_Server;
			queryUser = cfgs.Login_Ldap_QueryUser;
			queryPassword = SecurityUtils.Decrypt(cfgs.Login_Ldap_QueryPassword);
			domainSid = new SecurityIdentifier(cfgs.Login_Ldap_DomainSID);
		}

		private DirectorySearcher CreateDirectorySearcher() {
			DirectoryEntry de = new DirectoryEntry("LDAP://" + this.server, this.queryUser, this.queryPassword);
			return new DirectorySearcher(de, "");
		}

		public bool Authenticate(string user, string pwd) {
			//Valida credenciais.
			PrincipalContext ctx = new PrincipalContext(ContextType.Domain, server);
			if (!ctx.ValidateCredentials(user, pwd)) {
				ErrorMsg = "Usuário ou senha inválido.";
				return false;
			}
			//Recuperando informações do diretório
			DirectorySearcher ds = this.CreateDirectorySearcher();
			ds.Filter = "(&(objectClass=user)(|(name=" + user + ")(mail=" + user + ")(sAMAccountName=" + user + ")))";
			SearchResultCollection src = ds.FindAll();
			if (src.Count < 1) {
				ErrorMsg = "Usuário não encontrado no servidor de domínio.";
				return false;
			}
			if (src.Count > 1) {
				ErrorMsg = " Mais de um usuário encontrado com mesmo nome no servidor de dominio.";
				return false;
			}
			SearchResult sr = src[0];
			String sid = new SecurityIdentifier((Byte[])sr.Properties["objectSid"].OfType<Object>().FirstOrDefault(), 0).Value;
			String login = (String)sr.Properties["sAMAccountName"].OfType<Object>().FirstOrDefault();
			String name = (String)sr.Properties["cn"].OfType<Object>().FirstOrDefault();
			String email = (String)sr.Properties["userprincipalname"].OfType<Object>().FirstOrDefault();
			//Recuperando os grupos do diretorio
			HashSet<String> groups = new HashSet<String>();
			Dictionary<String, String> all = new Dictionary<String, String>();
			while (true) {
				groups.Clear();
				foreach (SearchResult sr2 in src) {
					String prim = (String)sr.Properties["primaryGroupID"].OfType<Object>().Select(x => x.ToString()).FirstOrDefault();
					if (!String.IsNullOrWhiteSpace(prim) && !all.ContainsKey(prim)) all.Add(prim, this.domainSid.ToString() + "-" + prim);
					foreach (String s in sr2.Properties["memberOf"]) {
						Match m = Regex.Match(s, "^CN=([^,]+),");
						if (!m.Success || m.Groups[1].Length == 0) continue;
						String gname = m.Groups[1].Value;
						if (all.ContainsKey(gname)) continue;
						groups.Add(gname);
					}
				}
				if (groups.Count == 0) break;
				ds.Filter = "(&(objectCategory=group)(|(name=" + String.Join(")(name=", groups) + ")))";
				src = ds.FindAll();
				foreach (SearchResult sr2 in src) {
					String gsid = new SecurityIdentifier((Byte[])sr2.Properties["objectSid"].OfType<Object>().FirstOrDefault(), 0).Value;
					String gname = (String)sr2.Properties["name"].OfType<Object>().FirstOrDefault();
					all.Add(gname, gsid);
				}
			}
			String[] groupSids = all.Values.Distinct().ToArray();
			//Validando login
			return this.TestLogin(sid, groupSids, sr);
		}

		private bool TestLogin(String sid, String[] groupSids, SearchResult sr) {
			IEnumerable<UserProfile> grp = UserProfile.FindBySids(cfg, groupSids);
			if (grp.Count() < 1) {
				ErrorMsg = "Usuário não pertence a nenhum grupo com permissão de acesso ao sistema.";
				return false;
			}
			UserK u = UserK.GetByForeignKey(cfg, sid);
			if (u == null) {
				if (sr == null) {
					//Recuperando informações do diretório
					DirectorySearcher ds = this.CreateDirectorySearcher();
					ds.Filter = "(&(objectCategory=user)(objectSid=" + (new SecurityIdentifier(sid)).ToLdapForm() + "))";
					sr = ds.FindOne();
				}//if
				String name = (String)sr.Properties["cn"].OfType<Object>().FirstOrDefault();
				String login = (String)sr.Properties["sAMAccountName"].OfType<Object>().FirstOrDefault();
				u = new UserK(cfg);
				u.Save(
					name,
					(String)sr.Properties["userprincipalname"].OfType<Object>().FirstOrDefault(),
					login,
					null,
					u.MakeInitials(name.Cleanup() ?? login),
					sid,
					grp
				);
			} else {
				u.SetProfiles(grp.Select(x => x.Id).ToArray());
			}
			UserK = u;
			return true;
		}

		public bool AuthenticateOnRegistry(string user, string pwd)
		{
			//Valida credenciais.
			PrincipalContext ctx = new PrincipalContext(ContextType.Domain, server);
			if (!ctx.ValidateCredentials(user, pwd))
			{
				ErrorMsg = "Usuário ou senha inválido.";
				return false;
			}
			//Recuperando informações do diretório
			DirectorySearcher ds = this.CreateDirectorySearcher();
			ds.Filter = "(&(objectClass=user)(|(name=" + user + ")(mail=" + user + ")(sAMAccountName=" + user + ")))";
			SearchResultCollection src = ds.FindAll();
			if (src.Count < 1)
			{
				ErrorMsg = "Usuário não encontrado no servidor de domínio.";
				return false;
			}
			if (src.Count > 1)
			{
				ErrorMsg = " Mais de um usuário encontrado com mesmo nome no servidor de dominio.";
				return false;
			}
			SearchResult sr = src[0];
			String sid = new SecurityIdentifier((Byte[])sr.Properties["objectSid"].OfType<Object>().FirstOrDefault(), 0).Value;
			String login = (String)sr.Properties["sAMAccountName"].OfType<Object>().FirstOrDefault();
			String name = (String)sr.Properties["cn"].OfType<Object>().FirstOrDefault();
			String email = (String)sr.Properties["userprincipalname"].OfType<Object>().FirstOrDefault();
			//Recuperando os grupos do diretorio
			HashSet<String> groups = new HashSet<String>();
			Dictionary<String, String> all = new Dictionary<String, String>();
			while (true)
			{
				groups.Clear();
				foreach (SearchResult sr2 in src)
				{
					String prim = (String)sr.Properties["primaryGroupID"].OfType<Object>().Select(x => x.ToString()).FirstOrDefault();
					if (!String.IsNullOrWhiteSpace(prim) && !all.ContainsKey(prim)) all.Add(prim, this.domainSid.ToString() + "-" + prim);
					foreach (String s in sr2.Properties["memberOf"])
					{
						Match m = Regex.Match(s, "^CN=([^,]+),");
						if (!m.Success || m.Groups[1].Length == 0) continue;
						String gname = m.Groups[1].Value;
						if (all.ContainsKey(gname)) continue;
						groups.Add(gname);
					}
				}
				if (groups.Count == 0) break;
				ds.Filter = "(&(objectCategory=group)(|(name=" + String.Join(")(name=", groups) + ")))";
				src = ds.FindAll();
				foreach (SearchResult sr2 in src)
				{
					String gsid = new SecurityIdentifier((Byte[])sr2.Properties["objectSid"].OfType<Object>().FirstOrDefault(), 0).Value;
					String gname = (String)sr2.Properties["name"].OfType<Object>().FirstOrDefault();
					all.Add(gname, gsid);
				}
			}
			String[] groupSids = all.Values.Distinct().ToArray();
			//Validando login
			return this.TestLogin(sid, groupSids, sr);
		}
	}
}