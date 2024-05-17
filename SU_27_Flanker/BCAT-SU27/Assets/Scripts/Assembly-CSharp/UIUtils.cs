using System.Collections.Generic;
using System.IO;
using System.Text;
using Rewired;
using UnityEngine;
using UnityEngine.UI;

public static class UIUtils
{
	private static List<string> badWordsArr;

	private static string google_badwords = "4r5e\r\n5h1t\r\n5hit\r\na55\r\nanal\r\nanus\r\nar5e\r\narrse\r\narse\r\nass\r\nass-fucker\r\nasses\r\nassfucker\r\nassfukka\r\nasshole\r\nassholes\r\nasswhole\r\na_s_s\r\nb!tch\r\nb00bs\r\nb17ch\r\nb1tch\r\nballbag\r\nballs\r\nballsack\r\nbastard\r\nbeastial\r\nbeastiality\r\nbellend\r\nbestial\r\nbestiality\r\nbi+ch\r\nbiatch\r\nbitch\r\nbitcher\r\nbitchers\r\nbitches\r\nbitchin\r\nbitching\r\nbloody\r\nblow job\r\nblowjob\r\nblowjobs\r\nboiolas\r\nbollock\r\nbollok\r\nboner\r\nboob\r\nboobs\r\nbooobs\r\nboooobs\r\nbooooobs\r\nbooooooobs\r\nbreasts\r\nbuceta\r\nbugger\r\nbum\r\nbunny fucker\r\nbutt\r\nbutthole\r\nbuttmunch\r\nbuttplug\r\nc0ck\r\nc0cksucker\r\ncarpet muncher\r\ncawk\r\nchink\r\ncipa\r\ncl1t\r\nclit\r\nclitoris\r\nclits\r\ncnut\r\ncock\r\ncock-sucker\r\ncockface\r\ncockhead\r\ncockmunch\r\ncockmuncher\r\ncocks\r\ncocksuck\r\ncocksucked\r\ncocksucker\r\ncocksucking\r\ncocksucks\r\ncocksuka\r\ncocksukka\r\ncok\r\ncokmuncher\r\ncoksucka\r\ncoon\r\ncox\r\ncrap\r\ncum\r\ncummer\r\ncumming\r\ncums\r\ncumshot\r\ncunilingus\r\ncunillingus\r\ncunnilingus\r\ncunt\r\ncuntlick\r\ncuntlicker\r\ncuntlicking\r\ncunts\r\ncyalis\r\ncyberfuc\r\ncyberfuck\r\ncyberfucked\r\ncyberfucker\r\ncyberfuckers\r\ncyberfucking\r\nd1ck\r\ndamn\r\ndick\r\ndickhead\r\ndildo\r\ndildos\r\ndink\r\ndinks\r\ndirsa\r\ndlck\r\ndog-fucker\r\ndoggin\r\ndogging\r\ndonkeyribber\r\ndoosh\r\nduche\r\ndyke\r\nejaculate\r\nejaculated\r\nejaculates\r\nejaculating\r\nejaculatings\r\nejaculation\r\nejakulate\r\nf u c k\r\nf u c k e r\r\nf4nny\r\nfag\r\nfagging\r\nfaggitt\r\nfaggot\r\nfaggs\r\nfagot\r\nfagots\r\nfags\r\nfanny\r\nfannyflaps\r\nfannyfucker\r\nfanyy\r\nfatass\r\nfcuk\r\nfcuker\r\nfcuking\r\nfeck\r\nfecker\r\nfelching\r\nfellate\r\nfellatio\r\nfingerfuck\r\nfingerfucked\r\nfingerfucker\r\nfingerfuckers\r\nfingerfucking\r\nfingerfucks\r\nfistfuck\r\nfistfucked\r\nfistfucker\r\nfistfuckers\r\nfistfucking\r\nfistfuckings\r\nfistfucks\r\nflange\r\nfook\r\nfooker\r\nfuck\r\nfucka\r\nfucked\r\nfucker\r\nfuckers\r\nfuckhead\r\nfuckheads\r\nfuckin\r\nfucking\r\nfuckings\r\nfuckingshitmotherfucker\r\nfuckme\r\nfucks\r\nfuckwhit\r\nfuckwit\r\nfudge packer\r\nfudgepacker\r\nfuk\r\nfuker\r\nfukker\r\nfukkin\r\nfuks\r\nfukwhit\r\nfukwit\r\nfux\r\nfux0r\r\nf_u_c_k\r\ngangbang\r\ngangbanged\r\ngangbangs\r\ngaylord\r\ngaysex\r\ngoatse\r\nGod\r\ngod-dam\r\ngod-damned\r\ngoddamn\r\ngoddamned\r\nhardcoresex\r\nhell\r\nheshe\r\nhoar\r\nhoare\r\nhoer\r\nhomo\r\nhore\r\nhorniest\r\nhorny\r\nhotsex\r\njack-off\r\njackoff\r\njap\r\njerk-off\r\njism\r\njiz\r\njizm\r\njizz\r\nkawk\r\nknob\r\nknobead\r\nknobed\r\nknobend\r\nknobhead\r\nknobjocky\r\nknobjokey\r\nkock\r\nkondum\r\nkondums\r\nkum\r\nkummer\r\nkumming\r\nkums\r\nkunilingus\r\nl3i+ch\r\nl3itch\r\nlabia\r\nlmfao\r\nlust\r\nlusting\r\nm0f0\r\nm0fo\r\nm45terbate\r\nma5terb8\r\nma5terbate\r\nmasochist\r\nmaster-bate\r\nmasterb8\r\nmasterbat*\r\nmasterbat3\r\nmasterbate\r\nmasterbation\r\nmasterbations\r\nmasturbate\r\nmo-fo\r\nmof0\r\nmofo\r\nmothafuck\r\nmothafucka\r\nmothafuckas\r\nmothafuckaz\r\nmothafucked\r\nmothafucker\r\nmothafuckers\r\nmothafuckin\r\nmothafucking\r\nmothafuckings\r\nmothafucks\r\nmother fucker\r\nmotherfuck\r\nmotherfucked\r\nmotherfucker\r\nmotherfuckers\r\nmotherfuckin\r\nmotherfucking\r\nmotherfuckings\r\nmotherfuckka\r\nmotherfucks\r\nmuff\r\nmutha\r\nmuthafecker\r\nmuthafuckker\r\nmuther\r\nmutherfucker\r\nn1gga\r\nn1gger\r\nnazi\r\nnigg3r\r\nnigg4h\r\nnigga\r\nniggah\r\nniggas\r\nniggaz\r\nnigger\r\nniggers\r\nnob\r\nnob jokey\r\nnobhead\r\nnobjocky\r\nnobjokey\r\nnumbnuts\r\nnutsack\r\norgasim\r\norgasims\r\norgasm\r\norgasms\r\np0rn\r\npawn\r\npecker\r\npenis\r\npenisfucker\r\nphonesex\r\nphuck\r\nphuk\r\nphuked\r\nphuking\r\nphukked\r\nphukking\r\nphuks\r\nphuq\r\npigfucker\r\npimpis\r\npiss\r\npissed\r\npisser\r\npissers\r\npisses\r\npissflaps\r\npissin\r\npissing\r\npissoff\r\npoop\r\nporn\r\nporno\r\npornography\r\npornos\r\nprick\r\npricks\r\npron\r\npube\r\npusse\r\npussi\r\npussies\r\npussy\r\npussys\r\nrectum\r\nretard\r\nrimjaw\r\nrimming\r\ns hit\r\ns.o.b.\r\nsadist\r\nschlong\r\nscrewing\r\nscroat\r\nscrote\r\nscrotum\r\nsemen\r\nsex\r\nsh!+\r\nsh!t\r\nsh1t\r\nshag\r\nshagger\r\nshaggin\r\nshagging\r\nshemale\r\nshi+\r\nshit\r\nshitdick\r\nshite\r\nshited\r\nshitey\r\nshitfuck\r\nshitfull\r\nshithead\r\nshiting\r\nshitings\r\nshits\r\nshitted\r\nshitter\r\nshitters\r\nshitting\r\nshittings\r\nshitty\r\nskank\r\nslut\r\nsluts\r\nsmegma\r\nsmut\r\nsnatch\r\nson-of-a-bitch\r\nspac\r\nspunk\r\ns_h_i_t\r\nt1tt1e5\r\nt1tties\r\nteets\r\nteez\r\ntestical\r\ntesticle\r\ntit\r\ntitfuck\r\ntits\r\ntitt\r\ntittie5\r\ntittiefucker\r\ntitties\r\ntittyfuck\r\ntittywank\r\ntitwank\r\ntosser\r\nturd\r\ntw4t\r\ntwat\r\ntwathead\r\ntwatty\r\ntwunt\r\ntwunter\r\nv14gra\r\nv1gra\r\nvagina\r\nviagra\r\nvulva\r\nw00se\r\nwang\r\nwank\r\nwanker\r\nwanky\r\nwhoar\r\nwhore\r\nwillies\r\nwilly\r\nxrated\r\nxxx";

	public static void SelectAllText(this InputField inputField)
	{
		inputField.ActivateInputField();
		inputField.caretPosition = inputField.text.Length;
		inputField.selectionAnchorPosition = 0;
		inputField.selectionFocusPosition = inputField.caretPosition;
	}

	public static string FormattedTime(float elapsedTime, bool ms = false)
	{
		int num = Mathf.FloorToInt(elapsedTime);
		int num2 = num % 60;
		int num3 = (num - num2) / 60;
		int num4 = num3 % 60;
		int num5 = (num3 - num4) / 60;
		if (ms)
		{
			int num6 = Mathf.RoundToInt((elapsedTime - (float)num) * 1000f);
			return string.Format("{0}:{1}:{2}.{3}", num5, num4.ToString("00"), num2.ToString("00"), num6.ToString("000"));
		}
		return string.Format("{0}:{1}:{2}", num5, num4.ToString("00"), num2.ToString("00"));
	}

	public static string GetHierarchyString(GameObject go)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(go.name);
		Transform transform = go.transform;
		while ((bool)transform.parent)
		{
			stringBuilder.Insert(0, "/");
			stringBuilder.Insert(0, transform.parent.gameObject.name);
			transform = transform.parent;
		}
		return stringBuilder.ToString();
	}

	public static string GetUnitName(Actor actor)
	{
		if (actor == null)
		{
			return "<uknown>";
		}
		string result = actor.actorName;
		Actor actor2 = actor;
		UnitSpawn unitSpawn = actor.unitSpawn;
		while (unitSpawn == null && (bool)actor2.parentActor)
		{
			actor2 = actor2.parentActor;
			unitSpawn = actor2.unitSpawn;
		}
		if ((bool)unitSpawn)
		{
			result = unitSpawn.unitSpawner.GetUIDisplayName();
		}
		return result;
	}

	public static Vector3 RewiredMouseInput()
	{
		Vector3 zero = Vector3.zero;
		if (ReInput.isReady)
		{
			Player player = ReInput.players.GetPlayer(0);
			zero.x = player.GetAxis("Mouse X");
			zero.y = player.GetAxis("Mouse Y");
			zero.z = player.GetAxis("Mouse Scroll");
		}
		return zero;
	}

	public static bool ContainsBadWord(string s)
	{
		if (badWordsArr == null)
		{
			badWordsArr = new List<string>();
			using StringReader stringReader = new StringReader(google_badwords);
			badWordsArr.Add(stringReader.ReadLine().Trim());
		}
		foreach (string item in badWordsArr)
		{
			if (s.Contains(item))
			{
				return true;
			}
		}
		return false;
	}
}
