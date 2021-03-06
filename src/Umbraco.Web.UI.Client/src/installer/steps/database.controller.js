angular.module("umbraco.install").controller("Umbraco.Installer.DataBaseController", function($scope, $http, installerService){
	
	$scope.checking = false;
	$scope.dbs = [
					{name: 'Embedded database SQL MEH', id: 0},
					{name: 'Microsft SQL Server', id: 1},
					{name: 'MySQL', id: 2},
					{name: 'Custom connection-string', id: -1}];

	if(installerService.status.current.model.dbType === undefined){
		installerService.status.current.model.dbType = 0;
	}
	
    $scope.validateAndForward = function(){
		if(!$scope.checking && this.myForm.$valid){
		 	$scope.checking = true;
			var model = installerService.status.current.model;

			$http.post(Umbraco.Sys.ServerVariables.installApiBaseUrl + "PostValidateDatabaseConnection",
				model).then(function(response){
					
					if(response.data === "true"){
						installerService.forward();	
					}else{
						$scope.invalidDbDns = true;
					}

					$scope.checking = false;
			}, function(){
				$scope.invalidDbDns = true;
				$scope.checking = false;
			});
		}
	};
});