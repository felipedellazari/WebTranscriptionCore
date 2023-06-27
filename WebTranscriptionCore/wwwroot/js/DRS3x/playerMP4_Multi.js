var mediaplayer = null;
var activeIndx = null;
var fileIndex = 0;

function start(guid) {
	mediaplayer = document.getElementById("mphtml5");

	mediaplayer.addEventListener("play", function () {
		intervalId = setInterval(function () {
			var currentTime = mediaplayer.currentTime;
			var addDuration = durationsToAdd[fileIndex];
			for (var i = 0; i < idxs.length; i++) {
				if (idxs[i].fileIndex == idxs[activeIndx].fileIndex) {
					if ((i + 1) < idxs.length) {
						if (currentTime + (addDuration / 10000000) < (idxs[i + 1].offset / 10000000)) {
							activeIndx = i;
							SetActive(idxs[i].id);
							break;
						}
					} else {
						activeIndx = i;
						SetActive(idxs[i].id);
					}
				}
			}
		}, 1000);
	}, false);

	mediaplayer.addEventListener("ended", function () {
		this.currentTime = 0;

		if (activeIndx != null && activeIndx < idxs.length - 1) {
			activeIndx++;
			PlayMedia(guid, idxs[activeIndx].id, true);
		}
		else {
			activeIndx = 0;
			fileIndex = 0;
			SetMediaPlayer(guid);
			SetActive(idxs[0].id);
		}
	}, false);

	PlayMedia(guid, idxs[0].id);
}

function SetMediaPlayer(guid, id = -1) {
	if (id >= 0) {
		mediaplayer.src = "../Session/PlayDRS3xVideo?id=" + guid + "&indx=" + id;
	} else {
		mediaplayer.src = "../Session/PlayDRS3xVideo?id=" + guid;
	}
	mediaplayer.type = "video/mp4";
	mediaplayer.load();
}

function PlayMedia(guid, id, play) {
	activeIndx = idxs.findIndex((el) => el.id == id);
	if (idxs[activeIndx].fileIndex > 0) {
		fileIndex = idxs[activeIndx].fileIndex;
	} else {
		fileIndex = 0;
	}
	SetActive(id);
	SetMediaPlayer(guid, id);

	if (!mediaplayer.paused) {
		mediaplayer.pause();
	}
	else if (play) {
		var indx = -1;

		idxs.some(function (obj, i) {
			if (obj.id == id)
				indx = i;
		});

		mediaplayer.currentTime = (indx < 1) ? 0 : idxs[indx].offsetEdited / 10000000;
		mediaplayer.play();
	}
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