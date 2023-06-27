var mediaplayer = null;
var intervalId = null;

function start(guid) {
	mediaplayer = document.getElementById("mphtml5");

	mediaplayer.addEventListener("ended", function () {
		clearInterval(intervalId);
		this.currentTime = 0;
		SetActive(idxs[0].id);
	}, false);

	mediaplayer.addEventListener("play", function () {
		intervalId = setInterval(function () {
			for (var i = 0; i < idxs.length; i++) {
				if ((i + 1) < idxs.length) {
					if (mediaplayer.currentTime < (idxs[i + 1].offset / 10000000)) {
						SetActive(idxs[i].id);
						break;
					}
				} else {
					SetActive(idxs[i].id);
				}
			}
		}, 1000);
	}, false);

	SetMediaPlayer(guid);
	PlayMedia(idxs[0].id);
}

function SetMediaPlayer(guid) {
	if (mediaplayer.src.length < 1) {
		mediaplayer.src = "../Session/PlayDRS3xVideo?id=" + guid;
		mediaplayer.type = "video/mp4";
		mediaplayer.load();
	}
}

function PlayMedia(id, play) {
	if (play) {
		var indx = -1;

		idxs.some(function (obj, i) {
			if (obj.id == id)
				indx = i;
		});

		if (!mediaplayer.paused) mediaplayer.pause();
		mediaplayer.currentTime = (indx < 1) ? 0 : idxs[indx].offset / 10000000;
		mediaplayer.play();
	}

	SetActive(id);
}

function SetActive(id) {
	for (var i = 0; i < idxs.length; i++) {
		document.getElementById(idxs[i].id).classList.remove("active");
	}
	document.getElementById(id).classList.add("active");
}

function EnableDownload() {
	document.getElementById("download").style.display = "block";
}

function DisableDownload() {
	document.getElementById("download").style.display = "none";
}