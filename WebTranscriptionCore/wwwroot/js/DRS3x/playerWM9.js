var mediaplayer = null;

function start(guid) {
	mediaplayer = document.getElementById("mpie");
	mediaplayer.URL = "../Session/PlayDRS3xVideo?id=" + guid;
	mediaplayer.controls.stop();

	setInterval(function () {
		for (var i = 0; i < idxs.length; i++) {
			if ((i + 1) < idxs.length) {
				if (mediaplayer.controls.currentPosition < (idxs[i + 1].offset / 10000000)) {
					SetActive(idxs[i].id);
					break;
				}
			} else {
				SetActive(idxs[i].id);
			}
		}
	}, 1000);
}

function PlayMedia(index, time, play) {
	mediaplayer.controls.currentPosition = time;

	if (play) {
		mediaplayer.controls.play();
	}

	SetActive(index);
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