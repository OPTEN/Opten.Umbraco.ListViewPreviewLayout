(function () {
	"use strict";

	function ListViewPreviewLayout($scope, listViewHelper, $location, $http) {

		$scope.clickItem = clickItem;

		function activate() {
			for (var i = 0; i < $scope.items.length; i++) {
				$scope.items[i].preview = false;
				$http.get("/umbraco/ListViewPreviewLayout/ContentRender/index/" + $scope.items[i].id).then(createUpdateItem(i));
			}
		}

		function createUpdateItem(index) {
			return function (response) {
				$scope.items[index].preview = response.data;
			};
		}

		function clickItem(item) {
			$location.path($scope.entityType + '/' + $scope.entityType + '/edit/' + item.id);

		}

		activate();

	}

	angular.module("umbraco").controller("OPTEN.ListViewPreviewLayout", ListViewPreviewLayout);

})();
