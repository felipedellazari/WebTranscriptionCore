if ($('#page-wrapper').height() < $(window).height() - $('#headerLogo').height()) {
    $('#page-wrapper').css("min-height", $(window).height() - $('#headerLogo').height());
}

$(window).resize(function () {
    if ($('#page-wrapper').height() < $(window).height() - $('#headerLogo').height()) {
        $('#page-wrapper').css("min-height", $(window).height() - $('#headerLogo').height());
    }
});

document.defaultAction = true;
document['onkeydown'] = detectEvent;
document['onkeypress'] = detectEvent;
document['onkeyup'] = detectEvent;

function detectEvent(e) {
    var evt = e || window.event;
    if (evt.code == "F12") {
        return false
    }
}

function mobileMenuNavigate() {
    $('#menucl').removeClass('collapse in');
    $('#menucl').addClass("collapse");
}

function loaderHidden(boolOption) {
   //alert('OLááááááááá');
   if (boolOption) {
      document.getElementById("loaderLayout").classList.add("hidden");
   } else {
      document.getElementById("loaderLayout").classList.remove("hidden");
	}
}
