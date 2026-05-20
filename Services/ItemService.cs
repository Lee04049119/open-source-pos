using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Models;
using Repositories;
using Repositories.Log;
using Services.Validations;

namespace Services
{

    public class ItemService : IItemService
    {
        private readonly IItemRepository _repo;
        private readonly ILogIt _log;



        public ItemService(IItemRepository repo, ILogIt log)
        {
            _repo = repo;
            _log = log;
        }
        

        

        public async Task<ServiceResponse> GetItemsAsync(string query, int companyId, int limit, int offset)
        {
            try
            {
                ServiceResponse response = new ServiceResponse();

                if (companyId <= 0)
                {
                    response.Flag = false;
                    response.IsValid = false;
                    response.Title = "Error!";
                    response.Message = "Invalid Value for company.";
                }
                else
                {
                    var items = await _repo.GetItemsAsync(query, companyId,limit,offset);

                    GetPosItemsModel model = new GetPosItemsModel();
                    model.Items = items;
                    model.Count = items.Count;

                    response.Flag = true;
                    response.Data = model;
                    response.IsValid = true;
                }
                return response;
            }
            catch (Exception ex)
            {
                _log.ExceptionLogFunc(ex);
                return new ServiceResponse
                {
                    IsValid = false,
                    Title = ServiceMessages.TitleFailure,
                    Message = ex.Message
                };
            }

        }

        public async Task<ServiceResponse> AddDataAsync(PosItem posItem)
        {
            try
            {
                if (posItem == null)
                    return InvalidBody();

                ServiceResponse vmServiceResponse = ServiceValidation.Validate(posItem, new PosItemValidator());

                int result = 0;

                if (vmServiceResponse.IsValid)
                {
                    
                    result = await _repo.AddDataAsync(posItem);
                    vmServiceResponse.Data = result;

                    if (result <= 0)
                    {
                        vmServiceResponse.Title = ServiceMessages.TitleFailure;
                        vmServiceResponse.Message = ServiceMessages.DataNotSaved;
                        vmServiceResponse.Flag = false;
                        vmServiceResponse.IsValid = false;
                    }
                    else
                    {
                        vmServiceResponse.Title = ServiceMessages.TitleSuccess;
                        vmServiceResponse.Message = ServiceMessages.DataSaved;
                        vmServiceResponse.Flag = true;
                        vmServiceResponse.IsValid = true;
                        vmServiceResponse.Data = result;
                    }
                }
                else
                {
                    vmServiceResponse.Title = ServiceMessages.TitleFailure;
                    vmServiceResponse.Message = ServiceErrorsMessages.DataInvalid;
                    vmServiceResponse.Flag = false;
                }

                return vmServiceResponse;


            }
            catch (Exception ex)
            {
                _log.ExceptionLogFunc(ex);
                return FailureResponse(ex.Message);
            }

        }
        public async Task<ServiceResponse> DeleteDataAsync(string itemId, int companyId)
        {
            try
            {
                var response = new ServiceResponse();
                if (string.IsNullOrWhiteSpace(itemId) || companyId <= 0)
                {
                    response.IsValid = false;
                    response.Title = ServiceMessages.TitleFailure;
                    response.Message = "Invalid item or company.";
                    return response;
                }

                var result = await _repo.DeleteDataAsync(itemId, companyId);
                if (result == -1)
                {
                    response.IsValid = false;
                    response.Title = ServiceMessages.TitleFailure;
                    response.Message = "Cannot delete: item is used on one or more invoices.";
                }
                else if (result <= 0)
                {
                    response.IsValid = false;
                    response.Title = ServiceMessages.TitleFailure;
                    response.Message = ServiceMessages.DataNotFound;
                }
                else
                {
                    response.IsValid = true;
                    response.Flag = true;
                    response.Title = ServiceMessages.TitleSuccess;
                    response.Message = "Item deleted.";
                }
                return response;
            }
            catch (Exception ex)
            {
                _log.ExceptionLogFunc(ex);
                return new ServiceResponse
                {
                    IsValid = false,
                    Title = ServiceMessages.TitleFailure,
                    Message = ex.Message
                };
            }
        }

        public async Task<ServiceResponse> UpdDataAsync(PosItem posItem)
        {
            try
            {
                if (posItem == null)
                    return InvalidBody();

                ServiceResponse vmServiceResponse = ServiceValidation.Validate(posItem, new PosItemUpdateValidator());

                int result = 0;

                if (vmServiceResponse.IsValid)
                {
                    result = await _repo.UpdDataAsync(posItem);
                    vmServiceResponse.Data = result;

                    if (result <= 0)
                    {
                        vmServiceResponse.Title = ServiceMessages.TitleFailure;
                        vmServiceResponse.Message = ServiceMessages.DataNotFound;
                        vmServiceResponse.Flag = false;
                        vmServiceResponse.IsValid = false;
                    }
                    else if (result > 1)
                    {
                        vmServiceResponse.Title = ServiceMessages.TitleFailure;
                        vmServiceResponse.Message = "More Rows Updated Than Expected.";
                        vmServiceResponse.Flag = false;
                        vmServiceResponse.IsValid = false;
                    }
                    else
                    {
                        vmServiceResponse.Title = ServiceMessages.TitleSuccess;
                        vmServiceResponse.Message = ServiceMessages.DataSaved;
                        vmServiceResponse.Flag = true;
                        vmServiceResponse.IsValid = true;
                    }
                }
                else
                {
                    vmServiceResponse.Title = ServiceErrorsMessages.Title;
                    vmServiceResponse.Message = ServiceErrorsMessages.DataInvalid;
                    vmServiceResponse.Flag = false;
                }

                return vmServiceResponse;


            }
            catch (Exception ex)
            {
                _log.ExceptionLogFunc(ex);
                return FailureResponse(ex.Message);
            }

        }

        private static ServiceResponse InvalidBody() => new ServiceResponse
        {
            IsValid = false,
            Title = ServiceMessages.TitleFailure,
            Message = "Request body is required."
        };

        private static ServiceResponse FailureResponse(string message) => new ServiceResponse
        {
            IsValid = false,
            Title = ServiceMessages.TitleFailure,
            Message = message
        };
    }
}
