yepnope({

  load: [
    'lib/jquery/jquery-2.0.3.min.js',
    
    /* 1.1.5 */
    'lib/angular/1.1.5/angular.min.js',
    'lib/angular/1.1.5/angular-cookies.min.js',
    'lib/angular/1.1.5/angular-mobile.min.js',
    'lib/angular/1.1.5/angular-mocks.js',
    'lib/angular/1.1.5/angular-sanitize.min.js',
    'lib/underscore/underscore.js',
    'js/umbraco.installer.js'
  ],

  complete: function () {
    jQuery(document).ready(function () {
        angular.bootstrap(document, ['umbraco.install']);
      });
  }
});