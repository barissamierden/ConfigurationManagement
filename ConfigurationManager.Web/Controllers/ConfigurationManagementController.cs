using Microsoft.AspNetCore.Mvc;
using Shared.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shared.Models;
using System.Linq;
using System;
using Shared.Enums;

namespace ConfigurationManager.Web.Controllers
{
    public class ConfigurationManagementController : Controller
    {
        private readonly IConfigurationRepository _configurationRepository;

        public ConfigurationManagementController(IConfigurationRepository configurationRepository)
        {
            _configurationRepository = configurationRepository;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetConfigurations()
        {
            var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
            var start = HttpContext.Request.Form["start"].FirstOrDefault();
            var length = HttpContext.Request.Form["length"].FirstOrDefault();
            var sortColumn = HttpContext.Request.Form["columns[" + HttpContext.Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
            var sortColumnDirection = HttpContext.Request.Form["order[0][dir]"].FirstOrDefault();
            var searchValue = HttpContext.Request.Form["search[value]"].FirstOrDefault();

            // Pagination
            int pageSize = length != null ? Convert.ToInt32(length) : 0;
            int skip = start != null ? Convert.ToInt32(start) : 0;

            // Total document count
            var recordsTotal = await _configurationRepository.GetCountAsync(searchText: searchValue);

            var data = await _configurationRepository.GetAllConfigurationItemsWithPaginationAsync(pageSize, skip, searchText: searchValue);

            return Json(new { draw = draw, recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = data });
        }

        [HttpPost]
        public async Task<IActionResult> CreateConfiguration(ConfigurationItem configurationItem)
        {
            try
            {
                configurationItem.Type = ((ConfigurationTypes)Convert.ToInt32(configurationItem.Type)).ToString();

                await _configurationRepository.InsertConfigItemAsync(configurationItem);

                return Json(new { success = true, message = "Konfigürasyon başarılı bir şekilde eklendi." });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return Json(new { success = false, message = "Konfigürasyon oluşturulurken bir hata oluştu!" });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateConfiguration(ConfigurationItem configurationItem)
        {
            try
            {
                configurationItem.Type = ((ConfigurationTypes)Convert.ToInt32(configurationItem.Type)).ToString();

                var response = await _configurationRepository.UpdateConfigItemAsync(configurationItem);

                if (response)
                {
                    return Json(new { success = true, message = "Konfigürasyon başarılı bir şekilde güncellendi." });
                }

                return Json(new { success = false, message = "Konfigürasyon güncellenirken bir hata oluştu!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return Json(new { success = false, message = "Konfigürasyon güncellenirken bir hata oluştu!" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfiguration(string id)
        {
            try
            {
                var response = await _configurationRepository.DeleteConfigItemAsync(id);

                if (response)
                {
                    return Json(new { success = true, message = "Konfigürasyon başarılı bir şekilde silindi." });
                }

                return Json(new { success = false, message = "Konfigürasyon silinirken bir hata oluştu!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return Json(new { success = false, message = "Konfigürasyon silinirken bir hata oluştu!" });
        }
    }
}
