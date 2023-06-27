(function ($) {
    "use strict";
    var mainApp = {
        metisMenu: function () {
            //METIS MENU 
            $('#main-menu').metisMenu();
        },
        loadMenu: function () {
            //LOAD APPROPRIATE MENU BAR
            $(window).bind("load resize", function () {
                if ($(this).width() < 768) {
                    $('div.sidebar-collapse').addClass('collapse')
                } else {
                    $('div.sidebar-collapse').removeClass('collapse')
                }
            });
        },
        slide_show: function () {
           //SLIDESHOW SCRIPTS

            $('#carousel-example').carousel({
                interval: 3000 // THIS TIME IS IN MILLI SECONDS
            })
        },
        reviews_fun: function () {
         //REVIEW SLIDE SCRIPTS
            $('#reviews').carousel({
                interval: 2000 //TIME IN MILLI SECONDS
            })
        },
        wizard_fun: function () {
        },
    };
    $(document).ready(function () {
        mainApp.metisMenu();
        mainApp.loadMenu();
        mainApp.slide_show();
        mainApp.reviews_fun();
        mainApp.wizard_fun();
    });
}(jQuery));